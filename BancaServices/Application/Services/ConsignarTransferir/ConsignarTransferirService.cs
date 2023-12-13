using BancaServices.Models.ConsignarAcuenta;
using Newtonsoft.Json.Linq;
using NLog;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using BancaServices.Domain.Interfaces;
using BancaServices.Domain.Interfaces.ConsignarTransferir;
using BancaServices.Application.Services.SerfiUtils;

namespace BancaServices.Application.Services.ConsignarTransferir
{
    public class ConsignarTransferir : IConsigarTransferir
    {
        private readonly NLog.ILogger logger = LogManager.GetCurrentClassLogger();
        private readonly ITransferenciasService transferenciasService;
        private readonly IConfiguration _configuration;

        public ConsignarTransferir(ITransferenciasService _transferenciasService, IConfiguration configuration)
        {
            _configuration = configuration;
            transferenciasService = _transferenciasService;
        }

        public async Task<JObject> ConsignarACuenta(ConsignarAcuentaRequest parameters)
        {
            JObject Result = new JObject();

            if (parameters.EsCuentaExterna)
            {
                JObject dataTransfe = new JObject();


                dataTransfe["identCliente"] = parameters.IdCliente;

                dataTransfe["tipoIdentCliente"] = SerfiUtils.Utils.HomologarTipoId(parameters.TipoId, SerfiUtils.Utils.Sistema.EIBS);

                dataTransfe["codBanco"] = parameters.CodBanco;

                dataTransfe["cuentaDestino"] = parameters.CuentaDestino;
                dataTransfe["tipoCtaDestino"] = parameters.TipoCuentaDestino;

                dataTransfe["cuentaOrigen"] = string.IsNullOrEmpty(parameters.CuentaOrigen) ? _configuration["AvanceCuentasExt_CuentaOrigen"] : parameters.CuentaOrigen;

                dataTransfe["valor"] = string.Format("{0:0}", parameters.MontoTotal).Replace(",", string.Empty);

                dataTransfe["canal"] = string.IsNullOrEmpty(parameters.CanalTransfer) ? _configuration["AvanceCuentasExt_Canal"] : parameters.CanalTransfer;

                dataTransfe["referencia"] = parameters.Referencia;

                dataTransfe["ipCreacion"] = parameters.Ip;
                dataTransfe["codPais"] = string.IsNullOrEmpty(parameters.CodPais) ? "CO" : parameters.CodPais;
                dataTransfe["usuarioCreacion"] = parameters.UsuarioCreacion;

                logger.Info($"Request Transferencia {dataTransfe.ToString(Formatting.None)}");
                JObject respuestaTranse = await transferenciasService.Transferir(dataTransfe, SerfiUtils.Utils.TiposTransferencias.OtrosBancos);

                if (respuestaTranse["estado"].Value<bool>())
                {
                    JObject datosRespuesta = new JObject
                    {
                        { "codRespuesta", "" },
                        { "descRespuesta", respuestaTranse["mensaje"].ToString() },
                        { "nroTransaccion", "" }
                    };

                    Result.Add("Codigo", "01");
                    Result.Add("Descripcion", "Transacción Exitosa");
                    Result.Add("Datos", datosRespuesta);
                }
                else
                {
                    Result.Add("Codigo", "02");
                    Result.Add("Descripcion", "Sin resultados");
                    Result.Add("RespTransfer", respuestaTranse);
                }
            }
            else
            {
                Result = await HacerCashin(parameters);
            }

            return Result;
        }

        public async Task<JObject> HacerCashin(ConsignarAcuentaRequest req)
        {

            JObject Result = new JObject();

            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    string urlCashIn = _configuration["urlCashIn"];

                    req.CodTRNCashin = string.IsNullOrEmpty(req.CodTRNCashin) ? _configuration["AvanceTCOCodTRN"] : req.CodTRNCashin;
                    req.CodCanalCashin = string.IsNullOrEmpty(req.CodCanalCashin) ? "7" : req.CodCanalCashin;


                    JObject data = new JObject();

                    data.Add("codTRN", req.CodTRNCashin);
                    data.Add("codCanal", req.CodCanalCashin);
                    data.Add("ctaDestino", req.CuentaDestino);

                    data.Add("monto", string.Format("{0:0.00}", req.MontoTotal).Replace(",", string.Empty));

                    if (!string.IsNullOrEmpty(req.CuentaOrigen))
                    {
                        data["ctaOrigen"] = req.CuentaOrigen;
                    }

                    logger.Info($"Request Cashin {data.ToString(Formatting.None)}");

                    HttpContent content = new StringContent(data.ToString());
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                    var response = await httpClient.PostAsync(urlCashIn, content);

                    var responseContent = await response.Content.ReadAsStringAsync();
                    JObject resultado = JsonConvert.DeserializeObject<JObject>(responseContent);

                    logger.Info($"Response Cashin {resultado.ToString(Formatting.None)}");

                    bool estadoRespuesta = bool.Parse(resultado["estado"].ToString());

                    if (estadoRespuesta)
                    {
                        JObject datosRespuesta = resultado.GetValue("data").Value<JObject>();

                        if (datosRespuesta.ContainsKey("codRespuesta") && datosRespuesta["codRespuesta"].ToString().Equals("0000"))
                        {

                            Result["Codigo"] = "01";
                            Result["Descripcion"] = "Transacción Exitosa";
                            Result["Datos"] = datosRespuesta;
                        }
                        else
                        {
                            Result["Codigo"] = "02";
                            Result["Descripcion"] = datosRespuesta["descRespuesta"].ToString();
                        }
                    }
                    else
                    {
                        Result = new JObject();
                        Result["Codigo"] = "02";
                        Result["Descripcion"] = "Sin resultados";
                    }

                    if (Result["Codigo"].ToString().Equals("02"))
                    {
                        Result["RespCashin"] = resultado;
                    }

                }
            }
            catch (Exception ex)
            {
                logger.Error<Exception>("Error HacerCashin Error={Error}", ex);
                Result = new JObject
                {
                    { "Codigo", "01" },
                    { "Descripcion", "Ocurrio un error" }
                };
            }

            return Result;

        }


        public async Task<JObject> ConsultarCuentasInscritas(string TipoDocumento, string NumeroDocumento, string CodigoPais = "CO", string CuentaAValidar = "")
        {
            JObject Result = new JObject();
            Result["Codigo"] = "01";
            Result["Descripcion"] = "OK";
            Result["Cuentas"] = new JArray();
            Result["CuentaValidada"] = false;

            try
            {
                string TipoDoc = SerfiUtils.Utils.HomologarTipoId(TipoDocumento, SerfiUtils.Utils.Sistema.EIBS);
                string URL = _configuration["URL_consultarCuentaInscrita"];
                JObject req = new JObject();
                req["identCliente"] = NumeroDocumento;
                req["tipoIdentCliente"] = TipoDoc;
                req["codPais"] = CodigoPais;

                var respCon = await ConsumirApiRest.ConsumirApiSalidaJObject(URL, req);

                if (respCon != null && respCon.ContainsKey("estado"))
                {
                    if (bool.Parse(respCon["estado"].ToString()))
                    {
                        JArray Cuentas = JArray.Parse(respCon["data"].ToString());
                        Cuentas = JArray.Parse(Cuentas.Select(a => a["registro"]).ToArray().ToString());

                        Result["Cuentas"] = Cuentas;

                        if (!string.IsNullOrEmpty(CuentaAValidar))
                        {
                            Result["CuentaValidada"] = Cuentas.Any(a => a["numProducto"].ToString().Equals(CuentaAValidar));
                        }

                    }
                    else
                    {
                        Result["Codigo"] = "03";
                        Result["Descripcion"] = "No tiene cuentas inscritas";
                    }

                }
                else
                {
                    Result["Codigo"] = "02";
                    Result["Descripcion"] = "No se pueden validar las cuentas";

                }

            }
            catch (Exception ex)
            {
                logger.Error<Exception>("Error ConsultarCuentasInscritas Error={Error}", ex);
                Result = new JObject
                {
                    { "Codigo", "01" },
                    { "Descripcion", "Ocurrio un error" }
                };

            }

            return Result;
        }
    }
}
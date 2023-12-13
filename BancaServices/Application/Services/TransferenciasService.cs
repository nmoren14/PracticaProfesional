using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using System.Net.Http.Headers;
using BancaServices.Models.DTO;
using BancaServices.ExternalAPI;
using BancaServices.Domain.Interfaces;
using BancaServices;
using BancaServices.Application.Services.SerfiUtils;

namespace BancaServices.Application.Services
{
    public class TransferenciasService : ITransferenciasService
    {
        private readonly IProductosService productosService;
        private readonly NLog.ILogger logger = LogManager.GetCurrentClassLogger();
        private static readonly string APPSETTING_CANAL_WEB = "Canal_Tansferenica_Web";
        private static readonly string APPSETTING_CANAL_APP = "Canal_Tansferenica_App";
        private readonly IConfiguration _configuration;
        private readonly BancaServicesLogsEntities Context; // Inject the DbContext


        public TransferenciasService(IProductosService _productosService, BancaServicesLogsEntities dbContext, IConfiguration configuration)
        {
            productosService = _productosService;
            _configuration = configuration;
            Context = dbContext; // Assign the injected DbContext
        }
        public async Task<JObject> EstadoTransaccion(string idCliente)
        {
            JObject Result = new JObject();
            try
            {
                using (var httpClient = new HttpClient())
                {
                    string Url = _configuration["Url_Estado_Transferencia_CMS"];
                    Uri urlEstadoTransaccion = new Uri(string.Format("{1}{0}", idCliente, Url));
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    var response = await httpClient.GetAsync(urlEstadoTransaccion);
                    JObject resultado = await response.Content.ReadAsAsync<JObject>();
                    bool estadoRespuesta = resultado.GetValue("estado").Value<bool>();
                    if (estadoRespuesta)
                    {
                        JArray datosRespuesta = resultado.GetValue("data").Value<JArray>();
                        Result.Add("Codigo", "01");
                        Result.Add("Descripcion", "Consulta Exitosa");
                        Result.Add("Datos", datosRespuesta);
                    }
                    else
                    {
                        Result = new JObject();
                        Result.Add("Codigo", "02");
                        Result.Add("Descripcion", "Sin resultados");
                    }

                }
            }
            catch (Exception)
            {
                Result = new JObject();
                Result.Add("Codigo", "01");
                Result.Add("Descripcion", "Ocurrio un error");
            }
            return Result;
        }

        public async Task<JObject> Transferir(JObject parameters, Utils.TiposTransferencias tipoTransferencia)
        {
            JObject Result = new JObject();
            string tipoId = string.Empty;
            string idCliente = string.Empty;
            try
            {
                string operacion = string.Empty;
                switch (tipoTransferencia)
                {
                    case Utils.TiposTransferencias.Internas:
                        if (parameters.SelectToken("tipoCuenta") != null && !string.IsNullOrEmpty(parameters.SelectToken("tipoCuenta").ToString()))
                        {
                            var tipoCuenta = parameters.GetValue("tipoCuenta").Value<string>();
                            JObject tipoCuentaSvc = await productosService.ConsultaTipoCuenta(parameters.GetValue("cuentaDestino").Value<string>());
                            if (tipoCuentaSvc.SelectToken("codProducto") == null)
                            {
                                Result.Add("estado", false);
                                Result.Add("mensaje", "No se encontró el producto de destino, verifique el número e intentelo nuevamente.");
                                return Result;
                            }
                            if (tipoCuenta != tipoCuentaSvc["codProducto"].ToString())
                            {
                                Result.Add("estado", false);
                                Result.Add("mensaje", "El tipo de cuenta del producto no coincide con el tipo de cuenta seleccionado.");
                                return Result;
                            }
                        }
                        operacion = "transferenciasInternas";
                        break;
                    case Utils.TiposTransferencias.OtrosBancos:
                        operacion = "transaccionOtrosBancos";
                        //parameters.Add("retrieval", ObtenerConsecutivoTransfOtrosBancos("2"));
                        break;

                }

                if (parameters.ContainsKey("tipoCuenta"))
                {
                    parameters.Remove("tipoCuenta");
                }

                tipoId = parameters.GetValue("tipoIdentCliente").Value<string>();
                idCliente = parameters.GetValue("identCliente").Value<string>();
                string numProducto = parameters.GetValue("cuentaOrigen").Value<string>().Trim();

                var ctaCorriente = await BuscarCuentaCorriente(numProducto, tipoId, idCliente);
                if (ctaCorriente != null)
                {
                    var saldo = ctaCorriente.GetValue("saldo").Value<double>();
                    var cupoSobreGiro = ctaCorriente.GetValue("disponibleSobregiro").Value<double>();
                    var valorTranseferencia = parameters.GetValue("valor").Value<double>();
                    if (!puedeTransferir(saldo, cupoSobreGiro, valorTranseferencia))
                    {
                        Result.Add("estado", false);
                        Result.Add("mensaje", "No tiene saldo suficiente para realizar esta transacción");
                        return Result;
                    }
                }
                using (var httpClient = new HttpClient())
                {
                    string Url = _configuration["Url_Transferencia"];
                    Uri urlEstadoTransaccion = new Uri(string.Format("{0}{1}", Url, operacion));
                    var buffer = System.Text.Encoding.UTF8.GetBytes(parameters.ToString());
                    var byteContent = new ByteArrayContent(buffer);
                    byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    var response = await httpClient.PostAsync(urlEstadoTransaccion, byteContent);
                    JObject resultado = await response.Content.ReadAsAsync<JObject>();
                    logger.Info($"Respuesta Servicio Tansferencias {idCliente} {resultado.ToString()}");
                    bool estadoRespuesta = resultado.GetValue("estado").Value<bool>();
                    if (estadoRespuesta)
                    {

                        Result.Add("estado", estadoRespuesta);
                        Result.Add("mensaje", "Transacción exitosa");
                    }
                    else
                    {
                        string mensaje = "";
                        JObject datosRespuesta = resultado.GetValue("data").Value<JObject>();

                        Result.Add("estado", estadoRespuesta);

                        if (datosRespuesta.ContainsKey("msg"))
                        {
                            mensaje = datosRespuesta.GetValue("msg").Value<string>().ToUpper();
                            Result["mensaje"] = HomologarRespuestaTranferencia(mensaje);
                        }
                        else
                        {
                            Result["mensaje"] = datosRespuesta.ToString();
                        }


                    }

                }

            }
            catch (TimeoutException toutEx)
            {
                if (tipoTransferencia == Utils.TiposTransferencias.OtrosBancos)
                {
                    var transferencia = JsonConvert.DeserializeObject<TransferenciaOtrosBancosDTO>(parameters.ToString());
                    var transferenciaClient = ApiClientFactory.Create<ITransferenciaProtocolApi>(_configuration);
                    var reversar = await transferenciaClient.Reversar(transferencia);
                }
                logger.Error($"Error Transferir TimeoutException TipoId={tipoId} IdCliente={idCliente} {toutEx.ToString()}");
            }
            catch (Exception ex)
            {
                Result.Add("estado", false);
                Result.Add("mensaje", "Ocurrio un error al realizar la transferencia");
                logger.Error($"Error Transferir TipoId={tipoId} IdCliente={idCliente} {ex.ToString()}");
            }
            return Result;
        }

        private bool puedeTransferir(double saldo, double cupoSobregiro, double valorTransferencia)
        {
            double cupoTotal = saldo + cupoSobregiro;
            return cupoTotal >= valorTransferencia;
        }

        private async Task<JObject> BuscarCuentaCorriente(string numProducto, string tipoId, string idCliente)
        {
            try
            {
                JArray cuentasCorrientes = await productosService.ConsultaCuentaCorriente(Utils.HomologarTipoIdCMS(tipoId), idCliente, Utils.DETALLE);

                if (cuentasCorrientes.Count == 0)
                {
                    return null;
                }
                var cuentaCorriente = cuentasCorrientes.FirstOrDefault(x => x.Value<string>("numProducto") == numProducto).ToObject<JObject>();
                return cuentaCorriente;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return null;
            }
        }

        private string HomologarRespuestaTranferencia(string mensaje)
        {
            string mensajeHomologado = string.Empty;

            if (mensaje.Contains("TX DESHABILITADA"))
            {
                mensajeHomologado = "Transaccion desabilitada";
            }
            else if (mensaje.Contains("NUM TX EXC LIM DIA"))
            {
                mensajeHomologado = "Excede cantidad por día, configurado en el perfil transaccional del cliente.";
            }
            else if (mensaje.Contains("VR TX EXC LIM DIA"))
            {
                mensajeHomologado = "Excede el monto diario, configurado en el perfil transaccional del cliente.";
            }
            else if (mensaje.Contains("NUM TX EXC LIM MES"))
            {
                mensajeHomologado = "Excede la cantidad de transacciones mensuales.";
            }
            else if (mensaje.Contains("VR TX EXC LIM MES"))
            {
                mensajeHomologado = "Excede el monto mensual.";
            }
            else if (mensaje.Contains("TX EXC LIM OP CRED EFT DIA"))
            {
                mensajeHomologado = "Error cuando se excede el monto de Riesgo diario ->10.000.000.";
            }
            else if (mensaje.Contains("TX EXC LIM OP CRED EFT MES"))
            {
                mensajeHomologado = "Error cuando se excede el monto de Riesgo mensual ->50.000.000.";
            }
            else
            {
                mensajeHomologado = mensaje;
            }

            return mensajeHomologado;
        }


        private long ObtenerConsecutivoTransfOtrosBancos(string canal)
        {
            int consecutivo;

            consecutivo = Context.ParametrosGenerales.FirstOrDefault(x => x.NombreParametro.Equals("ConsecutivoTransOtrosBancos")).ValorNumerico.GetValueOrDefault();

            var sConsecutibo = string.Format("{0}{1}", canal, consecutivo.ToString().PadLeft(12, '0'));
            ActualizarConsecutivo();
            return long.Parse(sConsecutibo);
        }

        private void ActualizarConsecutivo()
        {
            try
            {

                var parametro = Context.ParametrosGenerales.Where(x => x.NombreParametro.Equals("ConsecutivoTransOtrosBancos")).FirstOrDefault();
                parametro.ValorNumerico += 1;
                Context.SaveChanges();

            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
        }

        public async Task<JObject> EstadoTransferencia(JObject d)
        {
            JObject res = new JObject();

            try
            {
                string url = _configuration["URL_GENESYS_ESTADO_TRANSFER"];

                string NumCuenta = "";
                if (d.ContainsKey("NumeroCuentaOrigen") && !string.IsNullOrEmpty(d["NumeroCuentaOrigen"].ToString()))
                {
                    NumCuenta = $"/{d["NumeroCuentaOrigen"]}";
                }

                string request = $"{url}/{d["TipoDocumento"]}/{d["NumeroDocumento"]}/{d["FechaInicial"]}/{d["FechaFinal"]}{NumCuenta}";
                JObject re = await ConsumirApiRest.ConsumirApiSalidaJObject(request, null, "GET");
                res["Codigo"] = re["data"]["codigo"].ToString().Equals("00") ? "01" : "02";
                res["Descripcion"] = $"{re["data"]["codigo"]}_{re["data"]["msg"]}";
                res["Data"] = re["data"]["respuesta"] != null ? JArray.Parse(re["data"]["respuesta"].ToString()) : new JArray();
            }
            catch (Exception ex)
            {
                res["Codigo"] = "02";
                res["Descripcion"] = "Error";
                res["DetalleError"] = ex.ToString();
            }

            return res;
        }
    }
}
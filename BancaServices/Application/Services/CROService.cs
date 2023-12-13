using BancaServices.ContextoTablas.Desembolso;
using BancaServices.Models;
using BancaServices.Models.CompraEnlinea;
using BancaServices.Models.ConsignarAcuenta;
using BancaServices.Models.Desembolsar;
using BancaServices.Models.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Data.Entity;
using System.Globalization;
using System.Net;
using IBM.Data.DB2.Core;
using System.Text;
using BancaServices.Domain.Interfaces;
using BancaServices.Domain.Interfaces.ConsignarTransferir;
using BancaServices.Application.Services.SerfiUtils;

namespace BancaServices.Application.Services
{
    public class CROService : ICROService
    {
        private readonly IASNetServices asNetServices;
        private readonly NLog.ILogger logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly IAgilService _agilServices;
        private readonly IConsigarTransferir _consigarTransferir;
        private readonly IConsultasCms _consultasCms;
        private readonly IAvanceService _avanceService;
        private readonly INotificacionesServices _notificacionesServices;
        private readonly IClienteServices _clienteServices;
        private readonly IProductosService _productosService;
        private readonly IBloqueoServices _bloqueoServices;
        private readonly IConfiguration _configuration;
        private readonly CryptoUtil cryptoUtil;
        private readonly BancaServicesLogsEntities Context; // Inject the DbContext



        public enum ESTADOS_DESEMBOLSO_CMS { APROBADO = 4, RECHAZADO = 5, PENDIENTE = 3 };

        // CROService
        public CROService(
            IConfiguration configuration,
            IASNetServices _asNetServices,
            IAgilService agilService,
            IConsigarTransferir consigarTransferir,
            IConsultasCms consultasCms,
            IAvanceService avanceService,
            INotificacionesServices notificacionesServices,
            IClienteServices clienteServices,
            IProductosService productosService,
            IBloqueoServices bloqueoServices
            )
        {
            // ... constructor body ...
        }

        // Use a primary constructor and provide default values for unused parameters
        public CROService(IConfiguration configuration, BancaServicesLogsEntities dbContext)
            : this(configuration, null, null, null, null, null, null, null, null, null)
        {
            Context = dbContext; // Assign the injected DbContext
        }


        public async Task<JObject> CupoDisponible(string idCliente, string tipoDocumento)
        {
            JObject respuesta = new JObject();
            string noCreditoRotativo;
            try
            {
                noCreditoRotativo = await _consultasCms.ObtenerNumRotativoPorDocumento(idCliente, tipoDocumento);

                if (noCreditoRotativo.Equals(""))
                {
                    respuesta.Add("codigo", "01");
                    respuesta.Add("descripcion", "No se encuentra Numero de Rotativo");
                    respuesta.Add("cupoDisponible", "0");
                    return respuesta;
                }

                noCreditoRotativo = await _consultasCms.ObtenerTarjetaPorNumRotativo(noCreditoRotativo);

                if (noCreditoRotativo.Equals(""))
                {
                    respuesta.Add("codigo", "01");
                    respuesta.Add("descripcion", "No se encuentra Numero de Tarjeta");
                    respuesta.Add("cupoDisponible", "0");
                    return respuesta;
                }

                double saldoDisponible = await asNetServices.ObtenerCupoProducto(noCreditoRotativo, ASNetServices.TipoProductos.Rotativo);
                if (!double.IsNaN(saldoDisponible))
                {

                    respuesta.Add("codigo", "00");
                    respuesta.Add("descripcion", "Transacción exitosa");
                    respuesta.Add("cupoDisponible", string.Format("{0:0.00}", saldoDisponible).Replace(',', '.'));

                }
                else
                {
                    respuesta.Add("codigo", "01");
                    respuesta.Add("descripcion", "No se encontró el rotativo");
                    respuesta.Add("cupoDisponible", "0");
                }


            }
            catch (Exception ex)
            {
                logger.Error(ex);
                logger.Error<string>(ex.StackTrace);
                respuesta.Add("codigo", "01");
                respuesta.Add("descripcion", "Ocurrio un Error");
                respuesta.Add("cupoDisponible", "0");
            }
            return respuesta;
        }

        public async Task<JObject> Desembolsar(DesembolsarRequest req)
        {

            int montoDesembolsar = 0;
            JObject Respuesta = new JObject();
            var EstadoCMS = ESTADOS_DESEMBOLSO_CMS.RECHAZADO;
            string CodigoRespuesta = string.Empty;
            string DescripcionRespuesta = string.Empty;
            string codigoDesembolsoCms = string.Empty;

            int montoMinimoDesembolso = 0;

            string respObtenerTipoRotativo = string.Empty;

            CreditoRotativoLogs nl = new CreditoRotativoLogs
            {
                FechaRegistro = DateTime.Now,
                TipoId = req.tipoId,
                IdCliente = req.idCliente,
                MontoDesembolso = req.montoDesembolo,
                NumCredito = req.numCredito,
                CodigoEstablecimeinto = req.codigoEstablecimiento,
                CodigoBanco = req.codigoBancoDestino,
                CuentaDestino = req.cuentaDestino
            };



            try
            {
                montoDesembolsar = int.Parse(req.montoDesembolo);
            }
            catch (Exception)
            {
                Respuesta.Add("codigo", "01");
                Respuesta.Add("descripcion", "Error en monto a desembolsar");
                return Respuesta;
            }


            try
            {
                if (!req.EsCuentaExterna)
                {

                    var RespcuentaActiva = await _productosService.ConsultaTipoCuenta(req.cuentaDestino);

                    if (RespcuentaActiva.ContainsKey("estado"))
                    {
                        string[] EstadosValidar = { "CUENTA ACTIVA", "ACEPTA SOLO DEPOSITOS" };
                        if (!EstadosValidar.Contains(RespcuentaActiva["estado"].ToString().Trim()))
                        {
                            Respuesta.Add("codigo", "01");
                            Respuesta.Add("descripcion", "Cuenta destino no posee estado valido - " + RespcuentaActiva["estado"].ToString());
                            return Respuesta;
                        }
                    }
                    else
                    {
                        Respuesta.Add("codigo", "01");
                        Respuesta.Add("descripcion", "No se puede obtener el estado de la cuenta destino");
                        return Respuesta;
                    }


                }

                montoMinimoDesembolso = await GetMontoMinimoDesembolso(req.tipoId, req.idCliente, req.numCredito);

                if (montoDesembolsar >= montoMinimoDesembolso)// Se valida Monto Minimo
                {
                    string tarjeta = await _consultasCms.ObtenerTarjetaPorNumRotativo(req.numCredito);
                    string NumeroReferencia = await _consultasCms.ObtenerReferenciaPorTarjetaoPorNumRotativo(req.numCredito);
                    respObtenerTipoRotativo = await _consultasCms.ObtenerTipoProductoPorNumRotativoOTC(req.numCredito);
                    bool TieneTarjetaAsociado = !string.IsNullOrEmpty(tarjeta);

                    if (respObtenerTipoRotativo.Equals("CR") || respObtenerTipoRotativo.Equals("CR2"))
                    {
                        req.numCuotas = "36";
                    }
                    else if (respObtenerTipoRotativo.Equals("CR3"))
                    {
                        req.numCuotas = "60";
                        tarjeta = "";
                    }

                    req.codigoEstablecimiento = "9999999998";
                    nl.NumeroDeCuotas = int.Parse(req.numCuotas);

                    if (!TieneTarjetaAsociado && !respObtenerTipoRotativo.Equals("CR3"))// Algunos CR3 no tienen tarjeta asociada.
                    {
                        Respuesta["codigo"] = "01";
                        Respuesta["descripcion"] = "Rotativo no asociado a una tarjeta";
                        return Respuesta;
                    }

                    if (req.EsCuentaExterna && string.IsNullOrEmpty(NumeroReferencia))
                    {
                        Respuesta["codigo"] = "01";
                        Respuesta["descripcion"] = "No se puede obtener el numero de referencia para realizar la transferencia";
                        return Respuesta;
                    }

                    //Desembolso en CMS
                    JObject ResDesembolsoCms = await DesembolsarCms(req.tipoId, req.idCliente, req.montoDesembolo, req.numCredito, req.codigoBancoDestino, req.cuentaDestino);

                    codigoDesembolsoCms = ResDesembolsoCms.GetValue("Codigo").ToString();

                    if (codigoDesembolsoCms.Equals("000"))
                    {
                        CompraEnlineaResponse ResDesembolsoAsNet = null;

                        string FechaVencimiento = string.Empty;
                        string cvc2 = string.Empty;

                        if (!string.IsNullOrEmpty(tarjeta))
                        {
                            ///metodo aparte Interfaz injectada servicio novedad 15
                            var datos = await DatosSeguridad(req.tipoId, req.idCliente, tarjeta);

                            if (!string.IsNullOrEmpty(datos["fechaVenc"].ToString()) && !string.IsNullOrEmpty(datos["cvc2"].ToString()))
                            {
                                FechaVencimiento = cryptoUtil.DecryptData(datos["fechaVenc"].ToString());
                                cvc2 = cryptoUtil.DecryptData(datos["cvc2"].ToString());
                            }
                            else
                            {
                                DescripcionRespuesta = datos["descripcion"].ToString();
                            }

                        }


                        if (respObtenerTipoRotativo.Equals("CR3") && string.IsNullOrEmpty(tarjeta))
                        {

                            ResDesembolsoAsNet = new CompraEnlineaResponse
                            {
                                codigoRespuesta = "OK000",
                                numeroAutorizacion = "00"
                            };

                            EstadoCMS = ESTADOS_DESEMBOLSO_CMS.PENDIENTE;
                        }
                        else
                        {


                            if (!string.IsNullOrEmpty(FechaVencimiento) && !string.IsNullOrEmpty(cvc2))
                            {

                                string tipoProducto = respObtenerTipoRotativo.Equals("CR3") ? AvanceService.TIPOPROD_TCO : AvanceService.TIPOPROD_ROTATIVO;
                                string numeroTarjeta = respObtenerTipoRotativo.Equals("CR3") ? req.numCredito : tarjeta;

                                //Desembolsar en ASNET
                                CompraEnlineaRequest RequestDto = new CompraEnlineaRequest
                                {
                                    codigoCanal = "214",
                                    codigoEntidad = "0423",
                                    codigoSwitch = "0214",
                                    pinblock = "",
                                    tipoProducto = tipoProducto,
                                    fechadeVencimiento = FechaVencimiento,//EEN LA DOC ES FORMATO AAMM
                                    filler1Track = "",
                                    filler2Track = "",
                                    cvc2 = cvc2,
                                    codigoDispositivo = "16",
                                    codTransaccion = AvanceService.CODTRANS_COMPRA_ROTATIVO,
                                    origenTransaccion = "N",
                                    modoEntradaPOS = "017",
                                    valorTransaccion = req.montoDesembolo,
                                    iva = "0",
                                    devolucion = "000",
                                    valorPropina = "000",
                                    numCuotas = req.numCuotas,
                                    codigoEstablecimiento = req.codigoEstablecimiento,
                                    codigoMCC = "5411",//Supermercados
                                    codConvenio = "35",
                                    canalNovedad = req.canalNovedad,
                                    terminalNovedadIp = req.IpCliente,
                                    descripcionOperacion = "Desembolso CR",
                                    numeroAutorizacion = "",
                                    codigoCentroTUYA = "000",
                                    numeroTarjeta = numeroTarjeta
                                };

                                ResDesembolsoAsNet = await _avanceService.CompraCretidoEnlinea(RequestDto);

                            }
                            else
                            {
                                EstadoCMS = ESTADOS_DESEMBOLSO_CMS.PENDIENTE;
                            }

                        }

                        Respuesta = new JObject();
                        double MontoTotal = double.Parse(req.montoDesembolo);

                        if (ResDesembolsoAsNet != null && ResDesembolsoAsNet.codigoRespuesta != null && ResDesembolsoAsNet.codigoRespuesta.Equals("OK000"))
                        {
                            CodigoRespuesta = "00";
                            DescripcionRespuesta = "Operacion exitosa";

                            ObtenerRespuestaExitosa(Respuesta, CodigoRespuesta, DescripcionRespuesta, codigoDesembolsoCms, nl, ResDesembolsoCms, ResDesembolsoAsNet);


                            ConsignarAcuentaRequest CashoTransfer;

                            //13-01-2022 por orden de la gente se quita transferencia a otro banco
                            //string CuentaOriginenDesembolso = _configuration["DesembolsoCuentasExt_CuentaOrigen"];
                            //string CanalDesembolso = _configuration["DesembolsoExt_Canal"];

                            string CodTrnDesembolso = _configuration["DesembolsoCodTRN"];

                            JObject resCashoTransfer = new JObject
                            {
                                ["Codigo"] = "",
                                ["Datos"] = new JObject()
                            };
                            resCashoTransfer["Datos"]["nroTransaccion"] = "";

                            if (req.EsCuentaExterna)
                            {
                                //13-01-2022 por orden de la gente se quita transferencia a otro banco
                                //string CodigoBancoIBS = await Utils.HomologarCodBancoCMSToIBS(req.codigoBancoDestino, logger);
                                //CodigoBancoIBS = string.IsNullOrEmpty(CodigoBancoIBS) ? req.codigoBancoDestino : CodigoBancoIBS;

                                //CashoTransfer = new ConsignarAcuentaRequest(req.idCliente, req.tipoId, CodigoBancoIBS,
                                //                                            req.TipoCuentaDestino, req.cuentaDestino, MontoTotal,
                                //                                            NumeroReferencia, req.IpCliente, req.UsuarioCreacion,
                                //                                            "CO", CanalDesembolso, CuentaOriginenDesembolso);//request transferencia
                                resCashoTransfer["Codigo"] = "01";
                            }
                            else
                            {
                                CashoTransfer = new ConsignarAcuentaRequest(MontoTotal, req.cuentaDestino, CodTrnDesembolso);//request cashin
                                resCashoTransfer = await _consigarTransferir.ConsignarACuenta(CashoTransfer);
                            }

                            if (resCashoTransfer["Codigo"].ToString().Equals("01"))
                            {

                                nl.CodigoCashIn = "00";
                                nl.NroTransaccionCashIn = resCashoTransfer["Datos"]["nroTransaccion"] != null ? resCashoTransfer["Datos"]["nroTransaccion"].ToString() : "";

                                if (!req.EsCuentaExterna)
                                {
                                    EstadoCMS = ESTADOS_DESEMBOLSO_CMS.APROBADO;
                                }

                            }
                            else
                            {
                                CodigoRespuesta = "01";
                                string RespTransfer = resCashoTransfer.ContainsKey("RespTransfer") ? resCashoTransfer["RespTransfer"]["mensaje"].ToString() : "";

                                string RespCashin = string.Empty;

                                if (resCashoTransfer.ContainsKey("RespCashin"))
                                {
                                    if (((JObject)resCashoTransfer["RespCashin"]).ContainsKey("data"))
                                    {
                                        RespCashin = $"{resCashoTransfer["RespCashin"]["data"]["codRespuesta"].ToString()}{resCashoTransfer["RespCashin"]["data"]["descRespuesta"].ToString()}";
                                    }
                                    else
                                    {
                                        RespCashin = resCashoTransfer["RespCashin"].ToString();
                                    }
                                }

                                DescripcionRespuesta = $"{resCashoTransfer["Codigo"].ToString()} - {RespTransfer}{RespCashin}";

                                nl.CodigoCashIn = "01";
                                nl.NroTransaccionCashIn = "";
                                EstadoCMS = ESTADOS_DESEMBOLSO_CMS.RECHAZADO;
                            }

                            await NotificarDesembolso(req.tipoId, req.idCliente, MontoTotal, EstadoCMS);

                        }
                        else
                        {

                            CodigoRespuesta = "01";

                            if (ResDesembolsoAsNet != null && ResDesembolsoAsNet.codigoRespuesta != null && ResDesembolsoAsNet.descripcionRespuesta != null)
                            {
                                DescripcionRespuesta = $"Error Autorizador {ResDesembolsoAsNet.codigoRespuesta} {ResDesembolsoAsNet.descripcionRespuesta}";

                                nl.CodigoAutorizador = ResDesembolsoAsNet.codigoRespuesta;
                            }
                            else
                            {
                                if (string.IsNullOrEmpty(DescripcionRespuesta))
                                {
                                    DescripcionRespuesta = ResDesembolsoAsNet != null ? JsonConvert.SerializeObject(ResDesembolsoAsNet) : "No se obtuvo respuesta de compra en linea";
                                }
                            }

                            EstadoCMS = ESTADOS_DESEMBOLSO_CMS.RECHAZADO;

                        }

                        if (!req.EsCuentaExterna)
                        {
                            var respUpdateCms = await ActualizarEstadoDesembolsoCms(EstadoCMS, req.idCliente, req.cuentaDestino, req.canalNovedad);
                            if (!respUpdateCms.Codigo.Equals("01"))
                            {
                                SaveLogDesembolso("ActualizarEstadoDesembolsoCms", req.tipoId, req.idCliente, respUpdateCms.ToString(), "Desembolso rotativo", ip: req.IpCliente);
                            }
                        }

                    }
                    else
                    {

                        CodigoRespuesta = "01";
                        DescripcionRespuesta = $"Error Cms {codigoDesembolsoCms} {ResDesembolsoCms.GetValue("Descripcion").ToString()}";
                        nl.CodigoCms = codigoDesembolsoCms;
                    }

                }
                else
                {
                    CodigoRespuesta = "01";
                    DescripcionRespuesta = "El monto debe ser mayor a $" + montoMinimoDesembolso;
                }
            }
            catch (Exception ex)
            {
                CodigoRespuesta = "01";
                DescripcionRespuesta = ex.Message;
                Respuesta["detalleError"] = ex.ToString();
                logger.Error($"Error Desembolsar IdCliente:{nl.IdCliente} Error: {ex}");
            }


            nl.CodigoRespuesta = CodigoRespuesta;
            nl.DescripcionRespuesta = DescripcionRespuesta;

            SaveLogRotativo(nl);

            Respuesta["codigo"] = CodigoRespuesta;
            Respuesta["descripcion"] = DescripcionRespuesta;

            if (!req.EsCuentaExterna && EstadoCMS.Equals(ESTADOS_DESEMBOLSO_CMS.APROBADO))
            {
                await ValidarPrimerDesembolso(
                    new JObject {
                                    {"tipoId",req.tipoId },
                                    {"idCliente",req.idCliente },
                                    {"cuenta",req.cuentaDestino }
                    }
                    );
            }

            return Respuesta;



        }

        private static void ObtenerRespuestaExitosa(JObject Respuesta, string CodigoRespuesta, string DescripcionRespuesta, string codigoDesembolsoCms, CreditoRotativoLogs nl, JObject ResDesembolsoCms, CompraEnlineaResponse ResDesembolsoAsNet)
        {
            Respuesta.Add("codigo", CodigoRespuesta);
            Respuesta.Add("descripcion", DescripcionRespuesta);
            Respuesta.Add("codigoAutorizador", ResDesembolsoAsNet.codigoRespuesta);
            Respuesta.Add("IdDesembolsoAutorizador", ResDesembolsoAsNet.numeroAutorizacion);
            Respuesta.Add("codigoCms", codigoDesembolsoCms);
            Respuesta.Add("IdDesembolsoCms", ResDesembolsoCms.GetValue("IdDesembolso").ToString());

            nl.CodigoRespuesta = CodigoRespuesta;
            nl.DescripcionRespuesta = DescripcionRespuesta;
            nl.CodigoAutorizador = ResDesembolsoAsNet.codigoRespuesta;
            nl.IdDesembolsoAutorizador = ResDesembolsoAsNet.numeroAutorizacion;
            nl.CodigoCms = codigoDesembolsoCms;
            nl.IdDesembolsoCms = ResDesembolsoCms.GetValue("IdDesembolso").ToString();
        }

        private async Task<JObject> DesembolsarCms(string tipoId, string idCliente, string montoDesembolo, string numCredito, string codigoBanco, string cuentaDestino)
        {
            JObject ResDesembolso = new JObject();
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                    string urlDesembolsoCMSconfig = _configuration["urlDesembolsoCMS"];
                    Uri urlDesembolsoCMS = new Uri(urlDesembolsoCMSconfig);

                    var parameters = new Dictionary<string, string> {
                        { "tpIdentificacion", tipoId },
                        { "numIdentificacion", idCliente },
                        { "montoDesembolso", montoDesembolo },
                        { "numCredito", numCredito },
                        {"codigoBanco", codigoBanco },
                        {"numeroCuenta",cuentaDestino },
                        //{"bin", numCredito.Substring(0,6) }//por orden de mperea 29-01-2021 se quema el bin 899900
                        {"bin", "899900" }
                    };

                    var jsonParameter = JsonConvert.SerializeObject(parameters, Formatting.Indented);
                    logger.Info<string>($"Request Desembolsar CMS {jsonParameter}");

                    var content = new StringContent(jsonParameter, Encoding.UTF8, "application/json");
                    var response = await httpClient.PostAsync(urlDesembolsoCMS, content);
                    string resultado = await response.Content.ReadAsStringAsync();

                    JObject incomingJsonRespone = JObject.Parse(resultado);

                    logger.Info<string>($"Respuesta DesembolsarCms: {resultado}");

                    string codigoEstado = incomingJsonRespone["response"].Value<string>("codigoEstado");
                    string descripcionEstado = incomingJsonRespone["response"].Value<string>("descripEstado");


                    if (codigoEstado.Equals("000"))
                    {
                        string idDesembolso = incomingJsonRespone["response"].Value<string>("idDesembolso");
                        ResDesembolso.Add("Codigo", codigoEstado);
                        ResDesembolso.Add("Descripcion", descripcionEstado);
                        ResDesembolso.Add("IdDesembolso", idDesembolso);
                    }
                    else
                    {
                        ResDesembolso.Add("Codigo", codigoEstado);
                        ResDesembolso.Add("Descripcion", descripcionEstado);
                        ResDesembolso.Add("IdDesembolso", "00000");
                    }
                    return ResDesembolso;
                }

            }
            catch (Exception ex)
            {
                logger.Error<Exception>("Error DesembolsarCms", ex);
                ResDesembolso = new JObject();
                ResDesembolso.Add("Codigo", "001");
                ResDesembolso.Add("Descripcion", "Error Interno");
                return ResDesembolso;
            }


        }

        private async Task<CodigoDescripcionErrorData<string>> ActualizarEstadoDesembolsoCms(ESTADOS_DESEMBOLSO_CMS Estado, string NumeroDocumento, string NumeroCuentaDestino, string CanalNovedad)
        {
            CodigoDescripcionErrorData<string> resp = new CodigoDescripcionErrorData<string>();
            DateTime fecha = DateTime.Now;
            string Fechahoy = fecha.ToString("yyyyMMdd");
            string Hora = fecha.ToString("HHmmss");
            try
            {
                JObject req = new JObject();
                req["Conexion"] = _configuration.GetConnectionString("FACT");
                req["Sql"] = $"select ddconse from phsdescli where ddfecde={Fechahoy} and ddnumid={NumeroDocumento} order by ddconse desc FETCH FIRST 1 ROWS ONLY";
                req["Op"] = "01";
                var resConse = await _consultasCms.QueryCMS(req);

                if (resConse["Codigo"].ToString().Equals("01"))
                {
                    string consecutivo = resConse["Data"][0]["ddconse".ToUpper()].ToString();

                    req = new JObject();
                    req["Conexion"] = _configuration.GetConnectionString("FACT");
                    req["Sql"] = $"update phsdescli set ddesdes={(int)Estado}, ddfenv={Fechahoy}, ddhenv={Hora}, dduenv='{CanalNovedad}' where ddnumid={NumeroDocumento} and ddfecde={Fechahoy} and ddnucue='{NumeroCuentaDestino}' and ddconse={consecutivo} ";
                    req["Op"] = "02";

                    var resUpdate = await _consultasCms.QueryCMS(req);

                    if (resUpdate["Codigo"].ToString().Equals("01"))
                    {
                        resp.Codigo = resUpdate["Codigo"].ToString();
                        resp.Descripcion = resUpdate["Descripcion"].ToString();
                    }
                    else
                    {
                        resp.Codigo = "02";
                        resp.Descripcion = "No se puede actualizar cms phsdescli";
                    }
                }
                else
                {
                    resp.Codigo = "02";
                    resp.Descripcion = "No se puede obtener el consecutivo cms phsdescli";
                    resp.DetalleError = resConse["Descripcion"].ToString();

                }



            }
            catch (Exception ex)
            {
                logger.Error<Exception>("Error ActualizarEstadoDesembolsoCms {Error}", ex);

                resp.Codigo = "02";
                resp.Descripcion = "Error ActualizarEstadoDesembolsoCms";
                resp.DetalleError = ex.ToString();
            }

            return resp;
        }

        private void SaveLogRotativo(CreditoRotativoLogs Log)
        {
            try
            {
                Context.CreditoRotativoLogs.Add(Log);
                Context.SaveChanges();

            }
            catch (Exception ex)
            {
                logger.Error<Exception>("Error SaveLogRotativo ={Error} ", ex);
            }

        }

        private void SaveLogDesembolso(string nombreOpcion, string tipoDocumento, string numeroDocumento, string respuesta, string solicitud = "", string ip = "")
        {
            try
            {
                DesembolsoLog Log = new DesembolsoLog(nombreOpcion, tipoDocumento, numeroDocumento, respuesta, solicitud, ip);

                Context.DesembolsoLogs.Add(Log);
                Context.SaveChanges();

            }
            catch (Exception ex)
            {
                logger.Error<Exception>("Error SaveLogDesembolso ={Error} ", ex);
            }

        }

        private async Task<int> GetMontoMinimoDesembolso(string TipoDocumento, string NumeroDocumento, string NumeroCredito)
        {
            int montoMinimoDesembolso = int.Parse(_configuration["montoMinimoDesembolso"]);

            var MontoMinimoResp = await ConsultarCR(TipoDocumento, NumeroDocumento);

            if (MontoMinimoResp != null && MontoMinimoResp["Codigo"].ToString().Equals("00") && MontoMinimoResp.ContainsKey("data") && MontoMinimoResp["data"]["cre"] != null)
            {
                JArray ListCreditos = JArray.Parse(MontoMinimoResp["data"]["cre"].ToString());
                string MontoMinimoDesem = ListCreditos.Where(a => a["numCredito"].ToString().Equals(NumeroCredito)).Select(a => a["montoMinimo"].ToString().Replace(".00", "")).FirstOrDefault();

                if (!string.IsNullOrEmpty(MontoMinimoDesem))
                {
                    montoMinimoDesembolso = int.Parse(MontoMinimoDesem);
                }
            }

            return montoMinimoDesembolso;
        }

        public async Task<double> CupoDisponiblePorNumProducto(string tipodId, string idCliente, string numeroProducto)
        {
            var saldoDisponible = double.MinValue;
            var hTipoId = SerfinanzaUtils.HomologarTipoId(tipodId, SerfinanzaUtils.Sistema.AUTO);
            try
            {
                if (!string.IsNullOrEmpty(numeroProducto))
                {
                    ConsultaCupoRotativo.CreditCardTRXIClient consultaCro = new ConsultaCupoRotativo.CreditCardTRXIClient();
                    ConsultaCupoRotativo.creditCardTRXRequest request = new ConsultaCupoRotativo.creditCardTRXRequest();
                    request.creditCardTRXRequestProcessing = new ConsultaCupoRotativo.creditCardTRXRequestProcessingDTO();
                    #region Asignacion de variables
                    request.creditCardTRXRequestProcessing.usrId = "admpintco";
                    request.creditCardTRXRequestProcessing.usrPsw = "#2016p1n";
                    request.creditCardTRXRequestProcessing.codigoCanal = "194";
                    request.creditCardTRXRequestProcessing.codigoEntidad = "0423";
                    request.creditCardTRXRequestProcessing.codigoAplicacion = "CR";
                    request.creditCardTRXRequestProcessing.codigoTransaccion = "41";
                    request.creditCardTRXRequestProcessing.codigoTerminal = "1";
                    request.creditCardTRXRequestProcessing.codigoEstablecimiento = "999999999999";
                    request.creditCardTRXRequestProcessing.fechaTransaccion = DateTime.Now.ToString("yyyyMMdd");
                    request.creditCardTRXRequestProcessing.horaTransaccion = DateTime.Now.ToString("HHmmss"); ;
                    request.creditCardTRXRequestProcessing.fechaEfectivaTransaccion = "0221101043";
                    request.creditCardTRXRequestProcessing.codigoSwitch = "0194";
                    request.creditCardTRXRequestProcessing.dispositivo = "05";
                    request.creditCardTRXRequestProcessing.numeroAuditoria = "101043";
                    request.creditCardTRXRequestProcessing.consecutivo = "000000000001";
                    request.creditCardTRXRequestProcessing.tipoTransaccion = "0200";
                    request.creditCardTRXRequestProcessing.nomUbicPOSAdqu = "";
                    request.creditCardTRXRequestProcessing.TRACKII = string.Format("{0}=", numeroProducto);
                    request.creditCardTRXRequestProcessing.numeroTarjeta = numeroProducto;
                    request.creditCardTRXRequestProcessing.tipoDocumento = hTipoId;
                    request.creditCardTRXRequestProcessing.numeroDocumento = idCliente;
                    request.creditCardTRXRequestProcessing.producto = "40";
                    request.creditCardTRXRequestProcessing.subtipo = "";
                    request.creditCardTRXRequestProcessing.valorTransaccion = "";
                    request.creditCardTRXRequestProcessing.numeroCuotas = "";
                    request.creditCardTRXRequestProcessing.filler = "";
                    #endregion
                    ConsultaCupoRotativo.executeProcessingResponse processingResponse = await consultaCro.executeProcessingAsync(request);
                    var respuestaServicio = processingResponse.creditCardTRXResponse.@return;
                    if (respuestaServicio.codigoRespuesta.Equals("OK000"))
                    {
                        foreach (var datoTarjetas in respuestaServicio.infoTarjetas)
                        {
                            double.TryParse(datoTarjetas.saldoDisponible, out saldoDisponible);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                saldoDisponible = double.NaN;
                logger.Error($"Error CupoDisponiblePorNumProducto {ex}");
            }

            return saldoDisponible;
        }


        public async Task<ResponseInscription> InscribirCuenta(string tipoId, string idCliente, JArray productos, Cliente cliente)
        {
            ResponseInscription result = new ResponseInscription();
            int rows = 0;
            try
            {
                List<string> excluirProductos = new List<string>();
                if (productos != null)
                {
                    excluirProductos = await ConsultarEnPHSDESCUD(productos, idCliente, tipoId);
                }
                foreach (JObject item in productos)
                {
                    string numProduct = item.GetValue("numProducto").Value<string>();
                    string tipoProducto = Convert.ToInt32(item.GetValue("tipoProducto").Value<string>()).ToString();
                    if (tipoProducto.Equals("5"))
                    {
                        //Ahorro
                        tipoProducto = "7";
                    }
                    else
                    {
                        //Corriente
                        tipoProducto = "1";
                    }
                    bool existe = excluirProductos.Where(a => a.Equals(numProduct + "-" + tipoProducto)).Any();
                    if (!existe)
                    {
                        if (cliente != null)
                        {
                            string nombre = cliente.PrimerNombre + " " + cliente.PrimerApellido + " " + cliente.SegundoApellido;
                            if (nombre.Length > 50)
                            {
                                nombre = nombre.Substring(0, 48);
                            }
                            InscribirCRO data = new InscribirCRO()
                            {
                                ciudad = "BARRANQUILLA",//  cliente.CiudadResidencia,
                                fecha = DateTime.Now,
                                direccion = cliente.DireccionResidencia,
                                email = cliente.CorreoElectronico,
                                fax = cliente.Celular,
                                telefono = cliente.Celular,
                                idCliente = idCliente,
                                tipoId = tipoId,
                                nombre = nombre,
                                numProd = numProduct.TrimStart().TrimEnd(),
                                tipoProd = tipoProducto
                            };
                            if (await InscribirEnPHSDESCUD(data))
                            {
                                rows++;
                            }
                        }
                        else
                        {
                            result.Codigo = "01";
                            result.Descripcion = "No se ha encontrado cliente";
                            result.TieneCuenta = true;
                            return result;
                        }
                    }
                }
                result.Codigo = "00";
                result.Descripcion = $"Exitoso {rows} productos agregados";
                result.TieneCuenta = true;
            }
            catch (Exception ex)
            {
                result.Codigo = "02";
                result.Descripcion = "Error " + ex.Message;
                result.TieneCuenta = true;
            }
            return result;
        }

        private async Task<List<string>> ConsultarEnPHSDESCUD(JArray apps, string idCliente, string tipoId)
        {
            List<string> productos = new List<string>();
            try
            {
                string connString = _configuration.GetConnectionString("FACT");
                using (DB2Connection conn = new DB2Connection(connString))
                {
                    string query = string.Format("SELECT CDNUCUE, CDTICUE FROM PHSDESCUD WHERE CDNUMID = {0} AND CDTIPID = {1}", idCliente, tipoId);

                    conn.Open();
                    using (DB2Command command = new DB2Command(query, conn))
                    {
                        command.CommandType = CommandType.Text;
                        var reader = await command.ExecuteReaderAsync();
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                foreach (JObject item in apps)
                                {
                                    string numProduct = item.GetValue("numProducto").Value<string>();
                                    string tipoProduct = Convert.ToInt32(item.GetValue("tipoProducto").Value<string>()).ToString();
                                    if (tipoProduct.Equals("5"))
                                    {
                                        //Ahorro
                                        tipoProduct = "7";
                                    }
                                    else
                                    {
                                        //Corriente
                                        tipoProduct = "1";
                                    }
                                    if (numProduct.Equals(reader.GetValue(0).ToString().Trim()) && tipoProduct.Equals(reader.GetValue(1).ToString().Trim()))
                                    {
                                        productos.Add(numProduct + "-" + tipoProduct);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
            return productos;
        }

        public async Task<bool> InscribirEnPHSDESCUD(InscribirCRO data)
        {
            bool result = false;
            int rows = 0;
            try
            {
                string connString = _configuration.GetConnectionString("FACT");
                using (DB2Connection conn = new DB2Connection(connString))
                {
                    string query = string.Format("INSERT INTO PHSDESCUD " +
                                    "(CDNUCUE, CDTICUE, CDNOMBR, CDTIBAN, CDCOBAN, CDCOBANT, CDNUMID, CDTIPID, CDDIREC, CDCIUDA, CDEMAIL, CDTELEF, CDNUFAX, CDESCUE, CDTIDE, CDFGRA, CDHGRA, CDUGRA, CDFMOD, CDHMOD, CDUMOD, CDFENV) " +
                                    "VALUES ('{0}', {1}, '{2}', '01', '163', 1163, {3}, {4}, '{5}', '{6}', '{7}', {8}, {9}, 3, 'ROTATIVO', {10}, '{11}', 'SER01PRY', 0, '0', '', 0)",
                                    data.numProd, data.tipoProd, data.nombre, data.idCliente, data.tipoId, data.direccion, data.ciudad, data.email, data.telefono, data.fax, data.fecha.ToString("yyyyMMdd"), data.fecha.ToString("HHmmss"));

                    conn.Open();
                    using (DB2Command command = new DB2Command(query, conn))
                    {
                        command.CommandType = CommandType.Text;
                        rows = await command.ExecuteNonQueryAsync();
                    }
                }
                if (rows > 0)
                {
                    result = true;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
            return result;
        }

        public async Task<ResponseInscription> CrearCTAH(string tipoId, string idCliente)
        {
            ResponseInscription respuesta = new ResponseInscription();
            try
            {
                RequestCrearCTAH request = ConstruirRequestCTAH(tipoId, idCliente);
                if (request != null)
                {
                    ResponseCrearCTAH responseCTAH = await CrearCTAH(request);
                    if (responseCTAH != null)
                    {
                        if (responseCTAH.estado)
                        {
                            respuesta.Codigo = "00";
                            respuesta.Descripcion = "Cuenta Creada: " + responseCTAH.data.cuenta;
                            respuesta.TieneCuenta = true;
                            #region envio sms notificacion agil
                            await _agilServices.NotificacionAgilSMS(tipoId, idCliente, responseCTAH.data.cuenta);
                            #endregion
                        }
                        else
                        {
                            respuesta.Codigo = "02";
                            respuesta.Descripcion = responseCTAH.data.error;
                            respuesta.TieneCuenta = false;
                        }
                    }
                    else
                    {
                        respuesta.Codigo = "01";
                        respuesta.Descripcion = "Servicio no disponible";
                        respuesta.TieneCuenta = false;
                    }
                }
                else
                {
                    respuesta.Codigo = "02";
                    respuesta.Descripcion = "No se ha podido crear CTAH";
                    respuesta.TieneCuenta = false;
                }
            }
            catch (Exception ex)
            {
                respuesta.Codigo = "02";
                respuesta.Descripcion = "Error: " + ex.Message;
                respuesta.TieneCuenta = false;
            }
            return respuesta;
        }

        private RequestCrearCTAH ConstruirRequestCTAH(string tipoId, string id)
        {
            RequestCrearCTAH request;
            var config = _configuration;
            try
            {
                string HomoTipoId = SerfinanzaUtils.HomologarTipoId(tipoId, SerfinanzaUtils.Sistema.EIBS);
                request = new RequestCrearCTAH()
                {
                    anoInicio = int.Parse(config["Activacion_Ctah_AnoInicio"]),
                    medioPago = config["Activacion_Ctah_MedioPago"],
                    codigoProducto = config["Activacion_Ctah_CodProducto"],
                    condicion = config["Activacion_Ctah_Condicion"],
                    cuentaDebitar = config["Activacion_Ctah_CuentaDebitar"],
                    cuota = int.Parse(config["Activacion_Ctah_Cuota"]),
                    diaInicio = int.Parse(config["Activacion_Ctah_DiaInicio"]),
                    firma = int.Parse(config["Activacion_Ctah_Firma"]),
                    frecuencia = config["Activacion_Ctah_Frecuencia"],
                    mesInicio = int.Parse(config["Activacion_Ctah_MesInicio"]),
                    monto = int.Parse(config["Activacion_Ctah_Monto"]),
                    plazo = int.Parse(config["Activacion_Ctah_Plazo"]),
                    tipoId = HomoTipoId,
                    numId = id
                };
            }
            catch (Exception ex)
            {
                return null;
            }
            return request;
        }
        private async Task<ResponseCrearCTAH> CrearCTAH(RequestCrearCTAH request)
        {
            ResponseCrearCTAH responseCtah = new ResponseCrearCTAH();
            try
            {
                string JsonString = JsonConvert.SerializeObject(request);
                string Url = _configuration["Url_Activacion_Ctah"];
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(Url);
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";
                httpWebRequest.Headers.Add(HttpRequestHeader.Authorization, string.Empty);

                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    streamWriter.Write(JsonString);
                    await streamWriter.FlushAsync();
                    streamWriter.Close();
                }
                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                if (httpResponse.StatusCode == HttpStatusCode.OK)
                {
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                    {
                        responseCtah = JsonConvert.DeserializeObject<ResponseCrearCTAH>(streamReader.ReadToEnd());
                    }
                }
            }
            catch (Exception ex)
            {
                return null;
            }
            return responseCtah;
        }
        public async Task<JObject> ConsultarCR(string tipoId, string idCliente)
        {
            JObject response = new JObject();
            ConsultaParametrosResponse consulta = new ConsultaParametrosResponse();
            try
            {
                JObject request = new JObject()
                {
                    {"typeId", tipoId },
                    {"clientId", idCliente }
                };
                string content = JsonConvert.SerializeObject(request);
                consulta = await ConsultarParametrosCR(content);
                if (consulta.data != null)
                {
                    foreach (var producto in consulta.data.cre)
                    {
                        string saldoCms = producto.disponible;
                        var tarjeta = await _consultasCms.ObtenerTarjetaPorNumRotativo(producto.numCredito);
                        var saldo = await CupoDisponiblePorNumProducto(tipoId, idCliente, tarjeta);
                        producto.disponible = saldo >= 0 ? (saldo / 100).ToString("0.00", CultureInfo.InvariantCulture) : saldoCms;
                    }
                    consulta.Codigo = "00";
                    consulta.Descripcion = "Exitoso";
                }
                else
                {
                    consulta.Codigo = "01";
                    consulta.Descripcion = "Ha ocurrido un error en el servicio CRO.";
                }
            }
            catch (Exception ex)
            {
                consulta = new ConsultaParametrosResponse();
                consulta.Codigo = "02";
                consulta.Descripcion = "Error: " + ex.Message;
            }
            var setting = new JsonSerializerSettings
            {
                ContractResolver = new OmitJsonPropertyNameContractResolver()
            };
            response = JObject.Parse(JsonConvert.SerializeObject(consulta, setting));

            return response;
        }

        /// <summary>
        /// Consulta de credito pre-aprobado
        /// </summary>
        /// <param name="tipoId"></param>
        /// <param name="idCliente"></param>
        /// <returns></returns>
        public async Task<JObject> ConsultarCRPre(string tipoId, string idCliente)
        {
            dynamic response = new JObject();
            string NumRotativo = string.Empty;
            string CupoD = string.Empty;

            try
            {
                string connString = _configuration.GetConnectionString("FACT");
                using (DB2Connection conn = new DB2Connection(connString))
                {
                    using (DB2Command command = new DB2Command())
                    {
                        command.Connection = conn;
                        command.CommandText = @"SELECT T.H3F3VA AS Cupo, T.H3NRTA AS Producto FROM PHYESAT T WHERE T.H3CDTP LIKE 'CR%' 
                                                        AND T.H3UENB=10
                                                        AND T.H3UNNB= @idCliente
                                                        AND T.H3CDTI = @tipoDoc";

                        command.Parameters.Add("@idCliente", idCliente);
                        command.Parameters.Add("@tipoDoc", tipoId);
                        conn.Open();

                        using (var reader = await command.ExecuteReaderAsync())
                        {

                            if (reader.HasRows)
                            {
                                response.Codigo = "00";
                                response.Descripcion = "Exitoso";
                                response.Data = new JArray();
                                DataTable dt = new DataTable();
                                dt.Load(reader);

                                string record = JsonConvert.SerializeObject(dt, new CustomDataTableConverter());


                                JArray array = JArray.Parse(record);
                                response.Data = array;
                            }
                            else
                            {
                                response.Codigo = "01";
                                response.Descripcion = "El cliente no cuenta con Credito Rotativo Pre-aprobado";
                                // response.Data = "";
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                logger.Error(ex);
                logger.Error<string>(ex.StackTrace);
                response.Codigo = "01";
                response.Descripcion = "Ocurrió un error: " + ex.Message;
                return response;
            }
            return response;
        }
        private async Task<ConsultaParametrosResponse> ConsultarParametrosCR(string content)
        {
            ConsultaParametrosResponse resultService = new ConsultaParametrosResponse();
            try
            {
                string Url = _configuration["Url_Parm_Desembolso"];
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(Url);
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Accept = "application/json";
                httpWebRequest.Method = "POST";
                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    streamWriter.Write(content);
                    await streamWriter.FlushAsync();
                    streamWriter.Close();
                }
                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                if (httpResponse.StatusDescription == "OK")
                {
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                    {
                        resultService.data = JsonConvert.DeserializeObject<ConsultaParametrosResponse.Data>(streamReader.ReadToEnd());
                    }
                }
                else
                {
                    resultService = new ConsultaParametrosResponse();
                    return resultService;
                }
            }
            catch (Exception)
            {
                resultService = new ConsultaParametrosResponse();
                return resultService;
            }
            return resultService;
        }

        public async Task<JObject> DatosSeguridad(string tipoId, string idCliente, string tarjeta)
        {
            var response = new JObject();
            response["cvc2"] = "";
            response["fechaVenc"] = "";
            response["descripcion"] = "";

            try
            {

                string IdCifrado = cryptoUtil.EncryptData(idCliente);
                string NumTarjetaCifrado = cryptoUtil.EncryptData(tarjeta);

                string usuario = cryptoUtil.EncryptData(_configuration["usuarioAutorizdor"]);
                string pass = cryptoUtil.EncryptData(_configuration["claveAutorizador"]);

                NovedadesBloqueoSR.NovedadesNoMon_WSClient client = new NovedadesBloqueoSR.NovedadesNoMon_WSClient();
                NovedadesBloqueoSR.NovedadNoMon_Dto novedad = new NovedadesBloqueoSR.NovedadNoMon_Dto();
                novedad.codigoCanal = "0214";
                novedad.codigoEntidad = "0423";
                novedad.numeroIdentificacion = IdCifrado;
                novedad.numeroTarjetaAsignado = NumTarjetaCifrado;
                novedad.tipoIdentificacion = tipoId;
                novedad.tipoNovedad = "15";


                if (string.IsNullOrEmpty(IdCifrado) || string.IsNullOrEmpty(NumTarjetaCifrado) || string.IsNullOrEmpty(usuario) || string.IsNullOrEmpty(pass))
                {
                    response["descripcion"] = "No se pueden obtener los datos se seguridad";
                }
                else
                {

                    var responseNove = await client.aplicarNovedadAsync(novedad, usuario, pass);
                    var responseNovedad = responseNove.Body.aplicarNovedadReturn;

                    response["cvc2"] = responseNovedad.filler;
                    response["fechaVenc"] = responseNovedad.fechaSolicitud;
                    response["descripcion"] = responseNovedad.descripcionRespuesta;
                }


            }
            catch (Exception ex)
            {
                response["descripcion"] = "No se pueden obtener los datos se seguridad";
                response["detalleError"] = ex.Message;
            }

            return response;
        }


        private async Task NotificarDesembolso(string TipoId, string Idcliente, double Total, ESTADOS_DESEMBOLSO_CMS EstadoCMS)
        {
            try
            {

                string MensajeEstado = string.Empty;

                switch (EstadoCMS)
                {
                    case ESTADOS_DESEMBOLSO_CMS.APROBADO:
                        MensajeEstado = "se realizo correctamente";
                        break;
                    case ESTADOS_DESEMBOLSO_CMS.PENDIENTE:
                        MensajeEstado = "se encuentra en proceso";
                        break;
                    default:
                        break;
                }

                if (!string.IsNullOrEmpty(MensajeEstado))
                {
                    string mensaje = _configuration["DesembolsoSMS"];
                    string monto = string.Format(new CultureInfo("es-CO"), "{0:c0}", Total);

                    var datosbasicos = await _clienteServices.ConsultaClienteEIBS(TipoId, Idcliente);
                    if (datosbasicos != null)
                    {

                        string celular = datosbasicos.Celular;
                        string Nombre = datosbasicos.PrimerNombre;

                        DateTime FechaHoy = DateTime.Now;
                        string fecha = FechaHoy.ToString("dd/MM/yyyy");
                        string hora = FechaHoy.ToString("HH:mm:ss");

                        mensaje = mensaje.Replace("XXX_NOMBRE_XXX", Nombre)
                                         .Replace("XXX_VALOR_XXX", monto)
                                         .Replace("XXX_FECHA_XXX", fecha)
                                         .Replace("XXX_HORAXXX", hora)
                                         .Replace("XXX_ESTADO_XXX", MensajeEstado);

                        await _notificacionesServices.EnviarSmsAsync(celular, mensaje);

                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }

        }

        private async Task ValidarPrimerDesembolso(JObject parameters)
        {
            try
            {
                if (parameters.ContainsKey("cuenta"))
                {
                    string tipoId = parameters.GetValue("tipoId").Value<string>();
                    string idCliente = parameters.GetValue("idCliente").Value<string>();
                    string cuenta = parameters.GetValue("cuenta").Value<string>();

                    var codigos = await Context.Parametros.AsNoTracking().Where(a => a.Sistema == "AVANCE" && a.DescripcionParametro == "CodigosProductos").FirstOrDefaultAsync();

                    string[] CodigosProductos = codigos.ValorParametro.Split(',');

                    var listaCuentas = await _productosService.ConsultarSaldosCTAH(idCliente);

                    if (listaCuentas.Any(a => a["numProducto"].ToString() == cuenta && CodigosProductos.Contains(a["producto"].ToString())))
                    {

                        var desembolsos = Context.CreditoRotativoLogs.AsNoTracking().Where(x => x.TipoId.Equals(tipoId))
                                                          .Where(x => x.IdCliente.Equals(idCliente))
                                                          .Where(x => x.CuentaDestino.Equals(cuenta))
                                                          .Where(x => x.CodigoRespuesta.Equals("00"))
                                                          .ToList();

                        if (desembolsos != null && desembolsos.Count == 1)
                        {
                            JObject result = await _bloqueoServices.BloquearCuentaAhorros(tipoId, idCliente, cuenta);

                            //se reutiliza el log de PrimerAvanceTCOLog
                            //porque eso tiene un job que desbloquea despues de un tiempo
                            try
                            {
                                PrimerAvanceTCOLog log = new PrimerAvanceTCOLog
                                {
                                    TipoDocumento = tipoId,
                                    NumeroDocumento = idCliente,
                                    Cuenta = cuenta,
                                    RespBloqueo = result.ToString(),
                                    Fecha = DateTime.Now,
                                    EstadoJob = 0,
                                    IntentosDesbloqueo = 0
                                };

                                Context.PrimerAvanceTCOLogs.Add(log);
                                await Context.SaveChangesAsync().ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                logger.Error<string>("No se puede guardar log bloqueo primer desembolso " + JsonConvert.SerializeObject(ex));
                            }

                        }

                    }
                    else
                    {
                        logger.Error<string>($"No se puede validar primer desembolso ConsultarSaldosCTAH no devolvió cuentas IdCliente: {idCliente}");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error<string>("No se puede validar primer desembolso " + JsonConvert.SerializeObject(ex));
            }



        }
    }
}
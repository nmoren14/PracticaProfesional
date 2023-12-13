using BancaServices.Domain.Interfaces;
using BancaServices.Domain.Interfaces.ConsignarTransferir;
using BancaServices;
using BancaServices.Models.CompraEnlinea;
using BancaServices.Models.ConsignarAcuenta;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using System.Data.Entity;
using System.Diagnostics;
using System.Globalization;
using BancaServices.Application.Services.SerfiUtils;

namespace BancaServices.Application.Services
{
    public class AvanceService : IAvanceService
    {
        public const string BIN_ROTATIVO_CR2 = "899902";
        public const string BIN_ROTATIVO = "899900";
        public const string TIPOPROD_ROTATIVO = "40";
        public const string TIPOPROD_TCO = "31";
        public const string CODTRANS_AVANCE_ROTATIVO = "15";
        public const string CODTRANS_AVANCE_TCO = "11";
        public const string CODTRANS_COMPRA_ROTATIVO = "33";
        public const string CODTRANS_COMPRA_TCO = "11";

        private readonly NLog.ILogger logger = LogManager.GetCurrentClassLogger();
        private readonly IClienteServices clienteServices;
        private readonly INotificacionesServices notificacionesServices;
        private readonly IProductosService productosService;
        private readonly IConsigarTransferir _consigarTransferir;
        private readonly IBloqueoServices _bloqueoServices;
        private readonly CryptoUtil cryptoUtil;
        private readonly Random RandomGenerator = new Random();
        private readonly IConfiguration _configuration;
        private readonly BancaServicesLogsEntities Context; // Inject the DbContext



        public AvanceService(IConfiguration configuration)
        {
            _configuration = configuration;
            cryptoUtil = new CryptoUtil(configuration);
        }
        // AvanceService
        public AvanceService(
            IClienteServices _clienteServices,
            INotificacionesServices _notificacionesServices,
            IProductosService productoService,
            IConsigarTransferir consigarTransferir,

            IBloqueoServices bloqueoServices)
        {
            clienteServices = _clienteServices;
            notificacionesServices = _notificacionesServices;
            productosService = productoService;
            _consigarTransferir = consigarTransferir;
            _bloqueoServices = bloqueoServices;
        }

        // Use a primary constructor and provide default values for unused parameters
        public AvanceService(
            IClienteServices _clienteServices,
            INotificacionesServices _notificacionesServices, BancaServicesLogsEntities dbContext)
            : this(_clienteServices, _notificacionesServices, null, null, null)
        {
            Context = dbContext; // Assign the injected DbContext
        }



        public async Task<JObject> RealizarAvance(JObject parameters)
        {
            string tipoId = string.Empty;
            string idCliente = string.Empty;
            string numerodeTarjeta = string.Empty;
            string cvc2 = string.Empty;
            string valorTransaccion = "";
            string fechadeVencimiento;
            string numCuotas = string.Empty;
            string numReferencia = string.Empty;
            string establecimiento = string.Empty;
            string terminalNovedad = string.Empty;
            string iva = string.Empty;
            string devolucion = string.Empty;
            string descuento = string.Empty;
            string canalNovedad = string.Empty;
            string CodigoRespcompraEnlinea;
            string descripcionRespuesta = string.Empty;

            string tipoProducto = string.Empty;
            string codTransaccion = string.Empty;

            string tipoTransaccion = "A";
            AvanceTcoLog log = new AvanceTcoLog();

            var watch = new Stopwatch();
            watch.Start();

            if (parameters.ContainsKey("tipoTransaccion"))
            {
                tipoTransaccion = parameters.Value<string>("tipoTransaccion");
            }

            JObject respuestaServicio = new JObject
            {
                ["codigo"] = "01",
                ["descripcion"] = ""
            };

            try
            {
                tipoId = parameters.GetValue("tipoId").Value<string>();
                idCliente = parameters.GetValue("idCliente").Value<string>();
                numerodeTarjeta = parameters.GetValue("tarjeta").Value<string>();
                if (numerodeTarjeta.StartsWith(BIN_ROTATIVO) || numerodeTarjeta.StartsWith(BIN_ROTATIVO_CR2))
                {
                    codTransaccion = tipoTransaccion == "A" ? CODTRANS_AVANCE_ROTATIVO : CODTRANS_COMPRA_ROTATIVO;
                    tipoProducto = TIPOPROD_ROTATIVO;
                    fechadeVencimiento = "0000";
                    cvc2 = "";
                }
                else
                {
                    codTransaccion = tipoTransaccion == "A" ? CODTRANS_AVANCE_TCO : CODTRANS_COMPRA_TCO;
                    tipoProducto = TIPOPROD_TCO;
                    cvc2 = parameters.GetValue("cvc2").Value<string>();
                    fechadeVencimiento = parameters.GetValue("fechadeVencimiento").Value<string>();
                }
                valorTransaccion = parameters.GetValue("total").Value<string>();
                numCuotas = parameters.GetValue("numCuotas").Value<string>();
                Random generator = new Random();
                numReferencia = generator.Next(0, 999999).ToString("D6");
                establecimiento = "9999999998";
                iva = parameters.GetValue("iva").Value<string>();
                devolucion = "000";
                descuento = "000";
                terminalNovedad = parameters.GetValue("ip").Value<string>();
                canalNovedad = parameters.GetValue("canalNovedad").Value<string>();
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                JObject error = new JObject
                {
                    { "codigoRespuesta", "ERRSERF01" },
                    { "descripcionRespuesta", "Tipo de solicitud no compatible" }
                };
                return error;
            }


            try
            {
                //validar bin excluido
                string binesExcluidos = _configuration["BIN_EXCLUIR_AVANCE"];
                string[] binesExcluidosArray = binesExcluidos.Split(',');
                if (binesExcluidosArray.Contains(numerodeTarjeta.Substring(0, 6)))
                {
                    logger.Info("Bin Excluido para avances " + idCliente);
                    respuestaServicio["codigo"] = "77";
                    respuestaServicio["descripcion"] = "Bin Excluido para avances";
                    return respuestaServicio;
                }


                //Valida si el valor de la trancaccion excede el maximo permitido por día
                if (tipoTransaccion == "A" && !PuedeAvanzar(tipoId, idCliente, numerodeTarjeta, decimal.Parse(valorTransaccion)))
                {
                    logger.Info("Excede el monto máximo por días");
                    respuestaServicio["codigo"] = "77";
                    respuestaServicio["descripcion"] = "Excede el monto máximo por días ";
                    return respuestaServicio;
                }

                CryptoUtil util = new CryptoUtil(_configuration);


                CompraEnlineaRequest RequestDto = new CompraEnlineaRequest();

                RequestDto.codigoCanal = "214";
                RequestDto.codigoEntidad = "0423";
                RequestDto.codigoSwitch = "0214";
                RequestDto.pinblock = "";
                RequestDto.tipoProducto = tipoProducto;
                RequestDto.fechadeVencimiento = fechadeVencimiento;//EEN LA DOC ES FORMATO AAMM
                RequestDto.filler1Track = "";
                RequestDto.filler2Track = "";
                RequestDto.cvc2 = cvc2;
                RequestDto.codigoDispositivo = "16";
                RequestDto.codTransaccion = codTransaccion;
                RequestDto.origenTransaccion = "N";
                RequestDto.modoEntradaPOS = "017";
                RequestDto.valorTransaccion = valorTransaccion;
                RequestDto.descuento = descuento;
                RequestDto.iva = iva;
                RequestDto.devolucion = devolucion;
                RequestDto.valorPropina = "000";
                RequestDto.numCuotas = numCuotas;
                RequestDto.codigoEstablecimiento = establecimiento;
                RequestDto.codigoMCC = "5411";//Supermercados
                RequestDto.codConvenio = "35";
                RequestDto.canalNovedad = canalNovedad;
                RequestDto.terminalNovedadIp = terminalNovedad;
                RequestDto.numReferencia = numReferencia;
                RequestDto.descripcionOperacion = "Avance a cuenta de ahorros";
                RequestDto.numeroAutorizacion = "";
                RequestDto.codigoCentroTUYA = "000";
                RequestDto.numeroTarjeta = numerodeTarjeta;


                var respCompras = await CompraCretidoEnlinea(RequestDto);


                CodigoRespcompraEnlinea = respCompras.codigoRespuesta.ToString();
                descripcionRespuesta = respCompras.descripcionRespuesta.ToString();

                if (!CodigoRespcompraEnlinea.Equals("ERRSERF02"))
                {
                    if (!CodigoRespcompraEnlinea.Equals("ERRSERF04"))
                    {

                        if (CodigoRespcompraEnlinea.Equals("OK000"))
                        {
                            JObject resConsignacion = null;

                            if (tipoTransaccion == "A")
                            {
                                ConsignarAcuentaRequest CashoTransfer;

                                var EsCuentaExterna = parameters["EsCuentaExterna"] != null ? parameters["EsCuentaExterna"].Value<bool>() : false;
                                string CuentaDestino = parameters.GetValue("cuenta").Value<string>();
                                double MontoTotal = parameters.GetValue("total").Value<double>();

                                if (EsCuentaExterna)
                                {
                                    string CodBanco = parameters.GetValue("codBanco").Value<string>();
                                    string TipoCuentaDestino = parameters.GetValue("tipoCtaDestino").Value<string>();
                                    string Referencia = parameters.GetValue("referencia").Value<string>();
                                    string UsuarioCreacion = parameters.GetValue("usuario").Value<string>();
                                    string cuentaOrigen = _configuration["AvanceCuentasExt_CuentaOrigen"];
                                    string canal = _configuration["AvanceCuentasExt_Canal"];

                                    CashoTransfer = new ConsignarAcuentaRequest(idCliente, tipoId, CodBanco, TipoCuentaDestino, CuentaDestino, MontoTotal, Referencia, terminalNovedad, UsuarioCreacion, "CO", canal, cuentaOrigen);//request transferencia
                                }
                                else
                                {
                                    CashoTransfer = new ConsignarAcuentaRequest(MontoTotal, CuentaDestino);//request cashin
                                }

                                resConsignacion = await _consigarTransferir.ConsignarACuenta(CashoTransfer);

                                logger.Info<string>("Respuesta Consignar a cuenta avance en cuenta." + resConsignacion.ToString());
                            }

                            string codigo = resConsignacion != null && tipoTransaccion == "A" ? resConsignacion.GetValue("Codigo").Value<string>() : "01";

                            respuestaServicio["codigoRespuesta"] = CodigoRespcompraEnlinea;
                            respuestaServicio["descripcionRespuesta"] = descripcionRespuesta;
                            respuestaServicio["horaAutorizacion"] = respCompras.horaAutorizacion == null ? "" : respCompras.horaAutorizacion.ToString();
                            respuestaServicio["numeroAutorizacion"] = respCompras.numeroAutorizacion == null ? "" : respCompras.numeroAutorizacion.ToString();
                            respuestaServicio["fechaAutorizacion"] = respCompras.fechaAutorizacion == null ? "" : respCompras.fechaAutorizacion.ToString();
                            respuestaServicio["numReferencia"] = numReferencia;

                            if (codigo.Equals("01"))
                            {
                                respuestaServicio["codigo"] = "00";
                                descripcionRespuesta = "Transacción exitosa";

                                var datosResEibs = tipoTransaccion == "A" ? resConsignacion.GetValue("Datos").Value<JObject>() : null;
                                log.CodRespuestaEibs = datosResEibs != null ? datosResEibs.GetValue("codRespuesta").Value<string>() : "";
                                log.DescRespuestaEibs = datosResEibs != null ? datosResEibs.GetValue("descRespuesta").Value<string>() : "";
                                log.NumeroTransaccion = datosResEibs != null ? datosResEibs.GetValue("nroTransaccion").Value<string>() : "";

                                //los datos de EIBS quedan vacios cuando es una compra en linea nadamas, ya que no se consume el de consignar
                                if (string.IsNullOrEmpty(log.DescRespuestaEibs) && resConsignacion != null)
                                {
                                    log.DescRespuestaEibs = resConsignacion.ToString(Formatting.None);
                                }

                                log.Cuotas = int.Parse(numCuotas);
                                await notificar(parameters);

                            }
                            else
                            {
                                respuestaServicio["codigo"] = "01";
                                CodigoRespcompraEnlinea = "01";
                                descripcionRespuesta = "Ocurrió un error al consignar a la cuenta";
                                respuestaServicio.Add("detalleError", resConsignacion["DetalleError"]);

                                log.DescRespuestaEibs = resConsignacion.ToString(Formatting.None);
                            }

                        }

                        log.NumeroAutorizacion = respCompras.numeroAutorizacion == null ? "" : respCompras.numeroAutorizacion.ToString();
                    }
                    else
                    {
                        descripcionRespuesta = "Error obteniendo informacion DB2 AS400";
                    }
                }

                respuestaServicio["descripcion"] = descripcionRespuesta;

                try
                {
                    log.TipoIdCliente = tipoId;
                    log.IdCliente = idCliente;
                    log.Tarjeta = numerodeTarjeta.Substring(numerodeTarjeta.Length - 4);
                    log.Total = decimal.Parse(valorTransaccion);
                    log.NumeroReferencia = numReferencia;
                    log.Ip = terminalNovedad;
                    log.Canal = canalNovedad;
                    log.CodigoRespuesa = CodigoRespcompraEnlinea;
                    log.DescripcionRespuesta = descripcionRespuesta;
                    log.Fecha = DateTime.Now.Date;
                    log.Hora = DateTime.Now.TimeOfDay;

                    Context.AvanceTcoLogs.Add(log);
                    await Context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    string logdata = log != null ? JsonConvert.SerializeObject(log) : "";
                    logger.Error<string>("Error al guardar en AvanceTcoLogs" + ex.ToString() + " DataLog: " + logdata);
                    NotificarErrorUtil.NotificarError("Error al guardar en AvanceTcoLogs", ex.ToString() + " DataLog: " + logdata, _configuration);
                }


                if (respuestaServicio != null && respuestaServicio["codigo"] != null && respuestaServicio["codigo"].ToString().Equals("00"))
                {
                    await ValidarPrimerAvance(parameters);
                }


            }
            catch (Exception ex)
            {
                logger.Error<string>("Error Realizar Avance" + ex.ToString());
                respuestaServicio["codigo"] = "01";
                respuestaServicio["descripcion"] = "Error General";
                respuestaServicio["detalleError"] = ex.ToString();
            }

            watch.Stop();
            logger.Info($"Tiempo Respuesta RealizarAvance o compra: {watch.ElapsedMilliseconds} milisegundos");

            return respuestaServicio;
        }


        /// <summary>
        /// Permite hacer una compra con TC y con CRO en linea para cro mandar campo numerodeCRO sino mandar numeroTarjeta
        /// </summary>
        /// <param name="req"> request que ya tiene algunos parametros por defecto</param>
        /// <returns></returns>
        public async Task<CompraEnlineaResponse> CompraCretidoEnlinea(CompraEnlineaRequest req)
        {

            CompraEnlineaResponse respuestaServicio = new CompraEnlineaResponse();

            try
            {
                req.numReferencia = string.IsNullOrEmpty(req.numReferencia) ? RandomGenerator.Next(0, 999999).ToString("D6") : req.numReferencia;
            }
            catch (Exception ex)
            {
                logger.Error($"Error Parametros CompraCretidoEnlinea TipoId={req.tipoId} IdCliente={req.idCliente} : {ex.ToString()}");
                respuestaServicio.codigoRespuesta = "ERRSERF01";
                respuestaServicio.descripcionRespuesta = "Tipo de solicitud no compatible";
                return respuestaServicio;
            }


            try
            {

                AsNetComprasSv.CompraCreditoRequest_Dto RequestDto = new AsNetComprasSv.CompraCreditoRequest_Dto();

                RequestDto.codigoCanal = string.IsNullOrEmpty(req.codigoCanal) ? "214" : req.codigoCanal;
                RequestDto.codigoEntidad = string.IsNullOrEmpty(req.codigoEntidad) ? "0423" : req.codigoEntidad;
                RequestDto.codigoSwitch = string.IsNullOrEmpty(req.codigoSwitch) ? "0214" : req.codigoSwitch;

                RequestDto.pinblock = req.pinblock;
                RequestDto.tipoProducto = req.tipoProducto;
                RequestDto.fechaVencimiento = req.fechadeVencimiento;//EEN LA DOC ES FORMATO AAMM

                RequestDto.filler1Track = req.filler1Track;
                RequestDto.filler2Track = req.filler2Track;
                RequestDto.cvc2 = string.IsNullOrEmpty(req.cvc2) ? null : cryptoUtil.EncryptData(req.cvc2);//cvc2

                RequestDto.codigoDispositivo = req.codigoDispositivo;
                RequestDto.codigoTransaccion = req.codTransaccion;
                RequestDto.origenTransaccion = req.origenTransaccion;

                RequestDto.modoEntradaPOS = req.modoEntradaPOS;
                RequestDto.valorTransaccion = string.Format("{0}00", req.valorTransaccion);
                RequestDto.descuentoTransaccion = req.descuento;

                RequestDto.valorIVA = string.Format("{0}00", req.iva);
                RequestDto.valorDevolucion = req.devolucion;
                RequestDto.valorPropina = req.valorPropina;

                RequestDto.numeroCuotas = req.numCuotas;
                RequestDto.fechaNovedad = DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
                RequestDto.horaNovedad = DateTime.Now.ToString("HHmmssfff", CultureInfo.InvariantCulture);

                RequestDto.codigoEstablecimiento = req.codigoEstablecimiento;
                RequestDto.codigoMCC = req.codigoMCC;
                RequestDto.codConvenio = req.codConvenio;

                RequestDto.canalNovedad = req.canalNovedad;
                RequestDto.terminalNovedad = req.terminalNovedadIp;
                RequestDto.numeroReferencia = req.numReferencia;

                RequestDto.descripcionOperacion = req.descripcionOperacion;
                RequestDto.numeroAutorizacion = req.numeroAutorizacion;
                RequestDto.codigoCentroTUYA = req.codigoCentroTUYA;

                RequestDto.numeroTarjeta = string.IsNullOrEmpty(req.numeroTarjeta) ? cryptoUtil.EncryptData(req.numerodeCRO) : cryptoUtil.EncryptData(req.numeroTarjeta);

                if (!RequestDto.numeroTarjeta.Equals("ERROR"))
                {
                    if (!RequestDto.fechaVencimiento.Equals("0"))
                    {
                        AsNetComprasSv.CompraCreditoBPO_WSClient cliente = new AsNetComprasSv.CompraCreditoBPO_WSClient();

                        AsNetComprasSv.compraCreditoLineaResponse response = null;
                        AsNetComprasSv.CompraCreditoResponse_Dto respuesta = null;


                        try
                        {
                            cliente.Endpoint.Binding.SendTimeout = TimeSpan.FromMinutes(4);
                            string usrId = cryptoUtil.EncryptData(_configuration["usuarioAutorizdor"]);
                            string usrPsw = cryptoUtil.EncryptData(_configuration["claveAutorizador"]);

                            response = await cliente.compraCreditoLineaAsync(usrId, usrPsw, RequestDto);
                            respuesta = response.Body.compraCreditoLineaReturn;

                            logger.Info($"Respuesta compraCreditoLineaAsync TipoId={req.tipoId} IdCliente={req.idCliente}: {respuesta.ToString()}");

                        }
                        catch (Exception e)
                        {
                            logger.Error($"Error CompraCretidoEnlinea TipoId={req.tipoId} IdCliente={req.idCliente} : {e.ToString()}");
                            respuestaServicio.codigoRespuesta = "ERRSERF03";
                            respuestaServicio.descripcionRespuesta = "Error servicio de compras";
                        }


                        respuestaServicio.codigoRespuesta = respuesta.codigoRespuesta == null ? "" : respuesta.codigoRespuesta.ToString();
                        respuestaServicio.descripcionRespuesta = respuesta.descripcionRespuesta == null ? "" : respuesta.descripcionRespuesta.ToString();
                        respuestaServicio.horaAutorizacion = respuesta.horaAutorizacion == null ? "" : respuesta.horaAutorizacion.ToString();
                        respuestaServicio.numeroAutorizacion = respuesta.numeroAutorizacion == null ? "" : respuesta.numeroAutorizacion.ToString();
                        respuestaServicio.fechaAutorizacion = respuesta.fechaAutorizacion == null ? "" : respuesta.fechaAutorizacion.ToString();


                    }
                    else
                    {
                        respuestaServicio.codigoRespuesta = "ERRSERF04";
                        respuestaServicio.descripcionRespuesta = "Fecha de vencimiento no puede ser 0";
                        logger.Error($"Error CompraCretidoEnlinea TipoId={req.tipoId} IdCliente={req.idCliente} : {respuestaServicio.descripcionRespuesta}");
                    }
                }
                else
                {
                    respuestaServicio.codigoRespuesta = "ERRSERF02";
                    respuestaServicio.descripcionRespuesta = "Error servicio de encripcion";
                    logger.Error($"Error CompraCretidoEnlinea TipoId={req.tipoId} IdCliente={req.idCliente} : {respuestaServicio.descripcionRespuesta}");
                }

            }
            catch (Exception ex)
            {
                logger.Error($"Error CompraCretidoEnlinea TipoId={req.tipoId} IdCliente={req.idCliente} : {ex.ToString()}");
                respuestaServicio.codigoRespuesta = "01";
                respuestaServicio.descripcionRespuesta = "Error General";
                respuestaServicio.detalleError = ex.ToString();
            }

            return respuestaServicio;
        }


        public bool PuedeAvanzar(string tipoId, string idCliente, string tarjeta, decimal montoAvanzar)
        {
            decimal maxDiario = decimal.Parse(_configuration["AvanceMaxDiario"]);
            decimal avanzadoHoy = 0;


            try
            {
                var avances = Context.AvanceTcoLogs.Where(x => x.TipoIdCliente.Equals(tipoId))
                                                    .Where(x => x.IdCliente.Equals(idCliente))
                                                    .Where(x => x.Tarjeta.Equals(tarjeta.Substring(tarjeta.Length - 4)))
                                                    .Where(x => x.CodigoRespuesa.Equals("OK000"))
                                                    .Where(x => DbFunctions.TruncateTime(x.Fecha) == DateTime.Today).ToList();
                avanzadoHoy = avances.Sum(x => x.Total);

                return montoAvanzar + avanzadoHoy <= maxDiario;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }

            return false;
        }






        /// PRIVATE  METHODS
        private async Task notificar(JObject data)
        {
            try
            {
                var tipoId = data.GetValue("tipoId").Value<string>();
                var idCliente = data.GetValue("idCliente").Value<string>();
                var celular = (await clienteServices.ConsultaClienteEIBS(tipoId, idCliente)).Celular;
                var numerodeTarjeta = data.GetValue("tarjeta").Value<string>();
                var monto = string.Format(new CultureInfo("es-CO"), "{0:c0}", data.GetValue("total").Value<double>());
                var mensaje = _configuration["AvanceCuentas_SMS"];
                var fecha = DateTime.Now.ToString("dd/MM/yyyy");
                var hora = DateTime.Now.ToString("HH:mm:ss");
                var isExternalAccount = data["EsCuentaExterna"] != null ? data["EsCuentaExterna"].Value<bool>() : false;

                if (isExternalAccount)
                {
                    mensaje = string.Format(mensaje, numerodeTarjeta.Substring(numerodeTarjeta.Length - 4), monto, fecha, hora, "Transferencia  a cuentas de otros bancos");
                }
                else
                {
                    mensaje = string.Format(mensaje, numerodeTarjeta.Substring(numerodeTarjeta.Length - 4), monto, fecha, hora, "Abono en cuenta");
                }

                await notificacionesServices.EnviarSmsAsync(celular, mensaje);
            }
            catch (Exception ex)
            {
                logger.Error(ex);

            }

        }

        private async Task ValidarPrimerAvance(JObject parameters)
        {
            try
            {
                if (parameters.ContainsKey("cuenta"))
                {
                    string tipoId = parameters.GetValue("tipoId").Value<string>();
                    string idCliente = parameters.GetValue("idCliente").Value<string>();
                    string numerodeTarjeta = parameters.GetValue("tarjeta").Value<string>();
                    string cuenta = parameters.GetValue("cuenta").Value<string>();

                    var codigos = await Context.Parametros.Where(a => a.Sistema == "AVANCE" && a.DescripcionParametro == "CodigosProductos").FirstOrDefaultAsync();

                    string[] CodigosProductos = codigos.ValorParametro.Split(',');

                    var listaCuentas = await productosService.ConsultarSaldosCTAH(idCliente);

                    if (listaCuentas.Any(a => a["numProducto"].ToString() == cuenta && CodigosProductos.Contains(a["producto"].ToString())))
                    {

                        var avances = Context.AvanceTcoLogs.Where(x => x.TipoIdCliente.Equals(tipoId))
                                                          .Where(x => x.IdCliente.Equals(idCliente))
                                                          .Where(x => x.Tarjeta.Equals(numerodeTarjeta.Substring(numerodeTarjeta.Length - 4)))
                                                          .Where(x => x.CodigoRespuesa.Equals("OK000"))
                                                          .Where(x => x.CodRespuestaEibs.Equals("0000"))
                                                          .ToList();

                        if (avances != null && avances.Count == 1)
                        {
                            JObject result = await _bloqueoServices.BloquearCuentaAhorros(tipoId, idCliente, cuenta);

                            //LOg
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
                                logger.Error<string>("No se puede guardar log bloqueo primer avance " + JsonConvert.SerializeObject(ex));
                            }


                        }


                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error<string>("No se puede validar primer avance " + JsonConvert.SerializeObject(ex));
            }


        }
    }
}
using BancaServices.Application.Services.SerfiUtils;
using BancaServices.Domain.Interfaces;
using BancaServices.Models.Billetera;
using Newtonsoft.Json.Linq;
using NLog;
using System.Diagnostics;

namespace BancaServices.Application.Services
{
    public class ASNetServices : IASNetServices
    {
        private readonly NLog.ILogger logger = LogManager.GetCurrentClassLogger();
        private readonly IConfiguration _configuration;

        public ASNetServices(IConfiguration configuration)
        {
            _configuration = configuration;
        }


        public async Task<double> ObtenerCupoProducto(string numeroProducto, TipoProductos tipoProducto)
        {
            var watch = new Stopwatch();
            watch.Start();
            ConsultaCupoRotativo.creditCardTRXResponseProcessingDTO respuestaReturn = new ConsultaCupoRotativo.creditCardTRXResponseProcessingDTO();
            try
            {


                string usrId = _configuration["usuarioAutorizdor"];
                string usrPsw = _configuration["claveAutorizador"];

                DateTime thisDay = DateTime.Now;
                string Hora = thisDay.ToString("HHmmss");
                string Fecha = thisDay.ToString("yyyyMMdd");
                ConsultaCupoRotativo.CreditCardTRXIClient consultaCro = new ConsultaCupoRotativo.CreditCardTRXIClient();
                ConsultaCupoRotativo.creditCardTRXRequest request = new ConsultaCupoRotativo.creditCardTRXRequest();

                request.creditCardTRXRequestProcessing = new ConsultaCupoRotativo.creditCardTRXRequestProcessingDTO();
                #region Asignacion de variables
                request.creditCardTRXRequestProcessing.usrId = usrId;
                request.creditCardTRXRequestProcessing.usrPsw = usrPsw;
                request.creditCardTRXRequestProcessing.codigoCanal = "194";
                request.creditCardTRXRequestProcessing.codigoEntidad = "0423";
                request.creditCardTRXRequestProcessing.codigoAplicacion = "CR";
                request.creditCardTRXRequestProcessing.codigoTransaccion = "41";
                request.creditCardTRXRequestProcessing.codigoTerminal = "1";
                request.creditCardTRXRequestProcessing.codigoEstablecimiento = "999999999999";
                request.creditCardTRXRequestProcessing.fechaTransaccion = Fecha;
                request.creditCardTRXRequestProcessing.horaTransaccion = Hora;
                request.creditCardTRXRequestProcessing.fechaEfectivaTransaccion = "0221101043";
                request.creditCardTRXRequestProcessing.codigoSwitch = "0194";
                request.creditCardTRXRequestProcessing.dispositivo = "05";
                request.creditCardTRXRequestProcessing.numeroAuditoria = "101043";
                request.creditCardTRXRequestProcessing.consecutivo = "000000000001";
                request.creditCardTRXRequestProcessing.tipoTransaccion = "0200";
                request.creditCardTRXRequestProcessing.nomUbicPOSAdqu = "";
                request.creditCardTRXRequestProcessing.TRACKII = numeroProducto + "=";
                request.creditCardTRXRequestProcessing.numeroTarjeta = numeroProducto;
                request.creditCardTRXRequestProcessing.tipoDocumento = "";
                request.creditCardTRXRequestProcessing.numeroDocumento = "";
                request.creditCardTRXRequestProcessing.producto = ((int)tipoProducto).ToString();
                request.creditCardTRXRequestProcessing.subtipo = "";
                request.creditCardTRXRequestProcessing.valorTransaccion = "";
                request.creditCardTRXRequestProcessing.numeroCuotas = "";
                request.creditCardTRXRequestProcessing.filler = "";
                #endregion
                ConsultaCupoRotativo.executeProcessingResponse respuestaServicio = await consultaCro.executeProcessingAsync(request);

                respuestaReturn = respuestaServicio.creditCardTRXResponse.@return;


            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }

            watch.Stop();
            logger.Info($"Tiempo Respuesta ObtenerCupoProducto: {watch.ElapsedMilliseconds} milisegundos");

            if (respuestaReturn.codigoRespuesta != null && respuestaReturn.codigoRespuesta.Equals("00"))
            {
                if (double.TryParse(respuestaReturn.saldoDisponible, out double SaldoParsed))
                {
                    return SaldoParsed / 100;
                }
                else
                {
                    return double.NaN;
                }

            }
            else
            {
                return double.NaN;
            }

        }

        public enum TipoProductos
        {
            TarjetaCredito = 31,
            Rotativo = 40
        }

        public async Task<JObject> ValidacionSeguridad(BilleteraSeguridadRequest data)
        {
            CryptoUtil util = new CryptoUtil(_configuration);
            JObject response = new JObject();
            AsNetSeguridadCVV2.aplicarTransaccionRequest request = new AsNetSeguridadCVV2.aplicarTransaccionRequest();
            AsNetSeguridadCVV2.TransaccionAPPDTO req = new AsNetSeguridadCVV2.TransaccionAPPDTO();
            string idUsuario = _configuration["usuarioAutorizdor"];
            string idClave = _configuration["claveAutorizador"];
            req.codigoAplicacion = "SW";
            req.codigoProcesamiento = "905000";
            req.fechaTransaccion = DateTime.Now.Date.ToString("yyyyMMdd");
            req.horaTransaccion = DateTime.Now.TimeOfDay.ToString("hhmmss");
            req.fechaEfectiva = DateTime.Now.Date.ToString("yyyyMMdd");
            req.codigoCanal = "WEB";
            req.codigoSwitch = "0423";
            req.codigoDispositivo = "02";
            req.consecutivo = DateTime.Now.ToString("yyyyMMddhhmm");
            req.tipoTransaccion = "";
            req.codigoEntidad = "0423";
            req.userId = util.EncryptData(idUsuario);
            req.passwordId = util.EncryptData(idClave);
            req.numeroTarjeta = util.EncryptData(data.numeroTarjeta);
            req.numeroDocumento = util.EncryptData(data.numeroDocumento);
            req.tipoDocumento = data.tipoDocumento;
            req.fechaVencimientoTarjeta = util.EncryptData(data.fechaVencimientoTarjeta);
            req.valorCVV2 = util.EncryptData(data.valorCVV2);
            req.valorTransaccion = "";
            req.numeroCuotas = "";
            req.estado = "";
            req.pinblockActual = "";
            req.pinblockNuevo = "";
            req.filler1 = "";
            req.filler2 = "";
            req.filler3 = "";

            if (!string.IsNullOrEmpty(req.numeroTarjeta) &&
                !string.IsNullOrEmpty(req.valorCVV2) &&
                !string.IsNullOrEmpty(req.fechaVencimientoTarjeta) &&
                !string.IsNullOrEmpty(req.numeroDocumento))
            {
                request.aplicarTransaccionRequest1 = req;
                AsNetSeguridadCVV2.TransaccionesProxyClient cliente = new AsNetSeguridadCVV2.TransaccionesProxyClient();
                AsNetSeguridadCVV2.AplicarTransaccionResponse1 res = new AsNetSeguridadCVV2.AplicarTransaccionResponse1();

                try
                {
                    res = await cliente.AplicarTransaccionAsync(request);
                    response = JObject.FromObject(res.aplicarTransaccionResponse.aplicarTransaccionResponse1);
                }
                catch (Exception e)
                {
                    logger.Error("Error ValidacionSeguridad Request={Request} Error={Error}", req, e);
                }
            }
            else
            {
                response.Add("codigo", "ERRSERF02");
                response.Add("descripcion", "Error servicio de encripción");
            }
            // Logs
            return response;
        }
    }
}
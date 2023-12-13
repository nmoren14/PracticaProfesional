using Newtonsoft.Json.Linq;
using NLog;
using System.Diagnostics;
using BancaServices.Models;
using BancaServices.Domain.Interfaces;
using BancaServices;
using BancaServices.Application.Services.SerfiUtils;

namespace BancaServices.Application.Services
{
    public class PseServices : IPseServices
    {
        private readonly ICashServices _cashServices;
        private readonly NLog.ILogger logger = LogManager.GetCurrentClassLogger();
        private readonly IConfiguration _configuration;
        private readonly BancaServicesLogsEntities Context; // Inject the DbContext

        public PseServices(ICashServices cashServices, BancaServicesLogsEntities dbContext, IConfiguration configuration)
        {
            _cashServices = cashServices;
            _configuration = configuration;
            Context = dbContext; // Assign the injected DbContext
        }

        public async Task<JObject> TransaccionPSE(TransaccionesPSE parameters)
        {
            var watch = new Stopwatch();
            watch.Start();

            JObject respuesta = new JObject();
            LogEventInfo logEventInfo = new LogEventInfo();
            logEventInfo.Level = NLog.LogLevel.Info;
            logEventInfo.Properties.Add("TransaccionPSE", parameters);
            CashRequestModel requestCashOut = new CashRequestModel();
            requestCashOut.codTRN = _configuration["CodigoTranPagoPSE"];
            if (parameters.Canal <= 0)
            {
                requestCashOut.codCanal = _configuration["CodigoCanalPagoPSE"];
            }
            else
            {
                requestCashOut.codCanal = parameters.Canal.ToString();
            }

            requestCashOut.ctaOrigen = parameters.NumeroCuenta;
            requestCashOut.monto = string.Format("{0:0.00}", parameters.Monto).Replace(",", string.Empty);

            CashResponseModel responseCashOut = await _cashServices.ChashOut(requestCashOut);
            watch.Stop();
            logEventInfo.Properties.Add("TiempoChashOut", $"Tiempo Respuesta ChashOut: {watch.ElapsedMilliseconds} milisegundos");

            logEventInfo.Properties.Add("CashOutResponse", responseCashOut);
            string codigoResp;
            if (responseCashOut.estado && responseCashOut.data.codRespuesta.Equals("0000"))
            {
                codigoResp = "00";
                respuesta.Add("codigo", codigoResp);
                respuesta.Add("descripcion", string.Format("{0}-{1}", responseCashOut.data.codRespuesta, responseCashOut.data.descRespuesta));
                respuesta.Add("codigo_transaccion", responseCashOut.data.nroTransaccion);

            }
            else
            {
                codigoResp = Utils.HomologarRespuestaACH(responseCashOut.data.codRespuesta).PadLeft(5, '0');
                respuesta.Add("codigo", codigoResp);
                respuesta.Add("descripcion", string.Format("{0}-{1}", responseCashOut.data.codRespuesta, responseCashOut.data.descRespuesta));
            }

            parameters.CodigoRespuesta = responseCashOut.data.codRespuesta;
            parameters.DescripcionRespuesta = responseCashOut.data.descRespuesta;

            watch.Start();
            await GuardarTransaccion(parameters);
            watch.Stop();
            logEventInfo.Properties.Add("TiempoGuardarTransaccion", $"Tiempo Respuesta GuardarTransaccion: {watch.ElapsedMilliseconds} milisegundos");

            logger.Log(logEventInfo);

            return respuesta;
        }

        private async Task GuardarTransaccion(TransaccionesPSE transaccion)
        {
            try
            {

                transaccion.Fecha = DateTime.Now.Date;
                transaccion.Hora = DateTime.Now.TimeOfDay;
                Context.TransaccionesPSEs.Add(transaccion);

                await Context.SaveChangesAsync();

            }
            catch (Exception dbEx)
            {
                logger.Log(NLog.LogLevel.Error, dbEx, "No se puede guardar en base de datos TrazabiltyCode: {TrazabiltyCode} {TransaccionesPSE} {Error}", transaccion.CodigoPSE, transaccion, dbEx);
                NotificarErrorUtil.NotificarError("Error TransaccionPSE", $"No se puede guardar en base de datos TrazabiltyCode: {transaccion.CodigoPSE} {dbEx}", _configuration);
            }
        }
    }
}
using Newtonsoft.Json.Linq;
using NLog;
using BancaServices.Domain.Interfaces;
using BancaServices.Application.Services.SerfiUtils;

namespace BancaServices.Application.Services
{
    public class NotificacionesServices : INotificacionesServices
    {
        private readonly NLog.ILogger logger = LogManager.GetCurrentClassLogger();
        private readonly IConfiguration _configuration;

        public NotificacionesServices(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public async Task EnviarSmsAsync(string celular, string mensaje)
        {

            var values = new JObject{
                    { "celular", celular },
                    { "mensaje", mensaje },
                    { "canal", "BancaService" }
                };


            try
            {
                JObject jResponse = await ConsumirApiRest.ConsumirApiSalidaJObject(_configuration["Url_EnvioSMS"].ToString(), values, logger: logger);
                if (jResponse != null)
                {
                    LogEventInfo logEvent = new LogEventInfo
                    {
                        Level = NLog.LogLevel.Info
                    };

                    logEvent.Properties.Add("Celular", celular);
                    logEvent.Properties.Add("SMS", mensaje);
                    logEvent.Properties.Add("Respuesta", jResponse.ToString());
                    logger.Log(logEvent);

                }
            }
            catch (Exception ex)
            {
                logger.Error<Exception>("Error EnviarSmsAsync Error={Error}", ex);
            }
        }

        public bool sendEmailCertificado(string email, string body, string asunto, bool isHtml, byte[] sendFile, string nameFile)
        {
            var res = ComunicacionUtil.EnviarEmail(_configuration, email, body, asunto, isHtml, sendFile, nameFile);
            return res.Enviado;
        }
    }
}
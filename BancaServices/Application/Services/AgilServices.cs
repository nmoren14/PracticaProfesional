using Newtonsoft.Json.Linq;
using BancaServices;
using BancaServices.Domain.Interfaces;

namespace BancaServices.Application.Services
{
    public class AgilServices : IAgilService
    {
        private readonly IClienteServices _clientesService;
        private readonly INotificacionesServices _notificacionesServices;
        private readonly BancaServicesLogsEntities Context; // Inject the DbContext

        public AgilServices(IClienteServices clienteServices, BancaServicesLogsEntities dbContext, INotificacionesServices notificacionesServices)
        {
            _clientesService = clienteServices;
            _notificacionesServices = notificacionesServices;
            Context = dbContext; // Assign the injected DbContext

        }
        public async Task NotificacionAgilSMS(string tipoId, string idCliente, string cuenta)
        {
            string mensaje = string.Empty;
            JObject request = new JObject();
            JObject response = new JObject();
            string fecha = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            try
            {
                // consumir datos basicos para extrare el numero telefonico
                request.Add("tipoId", tipoId);
                request.Add("idCliente", idCliente);
                request.Add("cuenta", cuenta);
                var responseDatosBasicso = await _clientesService.ConsultaClienteByDocIVR(tipoId, idCliente);
                request.Add("datosBasico", JObject.FromObject(responseDatosBasicso).ToString());
                if (responseDatosBasicso.Codigo.Equals("00"))
                {
                    response.Add("Codigo", "00");
                    response.Add("Descripcion", "exitoso");
                    mensaje = string.Format("Banco Serfinanza te confirma la apertura de tu cuenta de ahorros terminada en *{0}. {1}. En caso que no hayas realizado esta transaccion comunicate al 3135997000 inmediatamente.", cuenta.Substring(cuenta.Length - 4), fecha);
                    await _notificacionesServices.EnviarSmsAsync(responseDatosBasicso.TelefonoPrincipal, mensaje);
                }
            }
            catch (Exception ex)
            {
                response = new JObject();
                response.Add("Codigo", "01");
                response.Add("Descripcion", ex.ToString());
            }
            saveNotificacionAgilLog(nameof(NotificacionAgilSMS), request.ToString(), response.ToString());
        }

        public void saveNotificacionAgilLog(string action, string request, string response)
        {
            try
            {
                NotificacionAgilLog notificacionAgilog = new NotificacionAgilLog()
                {
                    Action = action,
                    Request = request,
                    Response = response,
                    Date = DateTime.Now.ToString()
                };
                Context.NotificacionAgilLogs.Add(notificacionAgilog);
                Context.SaveChanges();
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
    }
}
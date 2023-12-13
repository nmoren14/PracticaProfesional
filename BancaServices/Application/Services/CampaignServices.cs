using Newtonsoft.Json.Linq;
using NLog;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using BancaServices.Domain.Interfaces;
using BancaServices;

namespace BancaServices.Application.Services
{
    public class CampaignServices : ICampaignServices
    {
        private readonly NLog.ILogger logger = LogManager.GetCurrentClassLogger();
        private readonly IAgilService _agilServices;
        private readonly IConfiguration _configuration;
        private readonly BancaServicesLogsEntities Context; // Inject the DbContext

        public CampaignServices(IAgilService agilService, BancaServicesLogsEntities dbContext, IConfiguration configuration)
        {
            _agilServices = agilService;
            _configuration = configuration;
            Context = dbContext; // Assign the injected DbContext
        }
        public async Task<JObject> ActualizarClienteCampCuentaAhorros(CampCuentasAhorros clienteCamp)
        {
            JObject respuesta = new JObject();
            if (!IsAvilableTime())
            {
                respuesta.Add("codigo", "01");
                respuesta.Add("descripcion", "En estos momentos no podemos atender tu solicutd, intenta más tarde");
                return respuesta;
            }
            CampCuentasAhorros clienteActualizar;
            clienteActualizar = Context.CampCuentasAhorros.FirstOrDefault(x => x.IdCliente.Equals(clienteCamp.IdCliente) &&
                                                                               x.TipoId.Equals(clienteCamp.TipoId));

            var cuenta = "";
            var codigoRespuesta = "";
            if (clienteCamp.Estado.Equals("A"))
            {
                var resultActivacion = await ActivarCuentaEibs(clienteCamp.IdCliente, clienteCamp.TipoId);
                var estado = resultActivacion["estado"].Value<bool>();
                if (resultActivacion != null)
                {
                    codigoRespuesta = resultActivacion["data"]["codError"].Value<string>();

                    if (codigoRespuesta.Equals("0"))
                    {
                        cuenta = resultActivacion["data"]["cuenta"].Value<string>();
                    }
                    else
                    {
                        respuesta.Add("codigo", "03");
                        respuesta.Add("descripcion", "Ocurrió un error al activar tu cuenta de ahorross");
                        return respuesta;
                    }
                }
                else
                {
                    respuesta.Add("codigo", "01");
                    respuesta.Add("descripcion", "No se pudo activar la cuenta de ahorro");
                }




                if (clienteActualizar != null)
                {
                    clienteActualizar.FechaModificacion = DateTime.Now;
                    clienteActualizar.Direccion = clienteCamp.Direccion;
                    clienteActualizar.Email = clienteCamp.Email;
                    clienteActualizar.Ip = clienteCamp.Ip;
                    if (clienteCamp.Estado.Equals("A") && codigoRespuesta.Equals("0") || clienteCamp.Estado.Equals("R"))
                    {
                        clienteActualizar.Estado = clienteCamp.Estado;
                    }
                    clienteActualizar.CodigoRespEibs = codigoRespuesta;
                    clienteActualizar.NumCuentaCreada = cuenta;
                    int filas = await Context.SaveChangesAsync();
                    if (filas > 0)
                    {
                        if (clienteCamp.Estado.Equals("A") && codigoRespuesta.Equals("0") || clienteCamp.Estado.Equals("R"))
                        {
                            respuesta.Add("codigo", "00");
                            respuesta.Add("descripcion", "Transacción Exitosa");
                            respuesta.Add("cuenta", cuenta);
                            respuesta.Add("clienteActualizado", JToken.FromObject(clienteActualizar));

                            #region envio sms notificacion agil
                            await _agilServices.NotificacionAgilSMS(clienteCamp.TipoId, clienteCamp.IdCliente, cuenta);
                            #endregion
                        }
                    }
                    else
                    {
                        respuesta.Add("codigo", "02");
                        respuesta.Add("descripcion", "Ocurrió un error al registar tu información");
                    }
                }
            }

            return respuesta;
        }

        public CampCuentasAhorros ConsultaClienteCampCuentaAhorros(string idClient, string tipoId)
        {
            CampCuentasAhorros campCuentasAhorros = null;
            if (IsAvilableTime())
            {

                campCuentasAhorros = Context.CampCuentasAhorros.FirstOrDefault(x => x.TipoId.Equals(tipoId) && x.IdCliente.Equals(idClient) && x.Estado.Equals("N"));

            }

            return campCuentasAhorros;
        }

        private async Task<JObject> ActivarCuentaEibs(string id, string tipoId)
        {
            tipoId = SerfiUtils.Utils.HomologarTipoId(tipoId, SerfiUtils.Utils.Sistema.EIBS);
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var codigoProducto = _configuration["Activacion_Ctah_CodProducto"];
                    var firma = _configuration["Activacion_Ctah_Firma"];
                    var monto = _configuration["Activacion_Ctah_Monto"];
                    var cuota = _configuration["Activacion_Ctah_Cuota"];
                    var medioPago = _configuration["Activacion_Ctah_MedioPago"];
                    var cuentaDebitar = _configuration["Activacion_Ctah_CuentaDebitar"];
                    var frecuencia = _configuration["Activacion_Ctah_Frecuencia"];
                    var plazo = _configuration["Activacion_Ctah_Plazo"];
                    var diaInicio = _configuration["Activacion_Ctah_DiaInicio"];
                    var mesInicio = _configuration["Activacion_Ctah_MesInicio"];
                    var anoInicio = _configuration["Activacion_Ctah_AnoInicio"];
                    var condicion = _configuration["Activacion_Ctah_Condicion"];

                    var requestBody = new JObject();
                    requestBody.Add("tipoId", tipoId);
                    requestBody.Add("numId", id);
                    requestBody.Add("codigoProducto", codigoProducto);
                    requestBody.Add("firma", firma);
                    requestBody.Add("monto", monto);
                    requestBody.Add("cuota", cuota);
                    requestBody.Add("medioPago", medioPago);
                    requestBody.Add("cuentaDebitar", cuentaDebitar);
                    requestBody.Add("frecuencia", frecuencia);
                    requestBody.Add("plazo", plazo);
                    requestBody.Add("diaInicio", diaInicio);
                    requestBody.Add("mesInicio", mesInicio);
                    requestBody.Add("anoInicio", anoInicio);
                    requestBody.Add("condicion", condicion);

                    var buffer = System.Text.Encoding.UTF8.GetBytes(requestBody.ToString());
                    var byteContent = new ByteArrayContent(buffer);
                    byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    Uri urlResumenCms = new Uri(string.Format("{0}", _configuration["Url_Activacion_Ctah"]));
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    var response = await httpClient.PostAsync(urlResumenCms, byteContent);
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var resultado = JsonConvert.DeserializeObject<JObject>(responseContent);
                    logger.Info(resultado.ToString());
                    return resultado;


                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return null;
            }

        }


        private bool IsAvilableTime()
        {
            var avilableTime = _configuration["Activacion_Ctah_AvilableTime"];
            var avilableStartTime = avilableTime.Split('-')[0];
            var avilableEndTime = avilableTime.Split('-')[1];
            int startHour = int.Parse(avilableStartTime.Split(':')[0]);
            int startMinutes = int.Parse(avilableStartTime.Split(':')[1]);
            int endHour = int.Parse(avilableEndTime.Split(':')[0]);
            int endMinutes = int.Parse(avilableEndTime.Split(':')[1]);

            var startTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, startHour, startMinutes, 0);
            var endTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, endHour, endMinutes, 0);
            var currentTime = DateTime.Now;
            return currentTime >= startTime && currentTime <= endTime;

        }

    }


}
using BancaServices.Domain.Interfaces;
using BancaServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using System.Net.Http.Headers;

namespace BancaServices.Application.Services
{
    public class ActivacionServices : IActivacionServices
    {
        private readonly NLog.ILogger logger = LogManager.GetCurrentClassLogger();
        private readonly IConfiguration configuration;
        private readonly BancaServicesLogsEntities Context; // Inject the DbContext


        public ActivacionServices(IConfiguration configuration, BancaServicesLogsEntities dbContext)
        {
            this.configuration = configuration;
            Context = dbContext; // Assign the injected DbContext
        }
        public async Task<JObject> HomologarTarjeta(JObject parameters)
        {

            string origen = parameters.GetValue("origen").Value<string>();
            string tipoId = parameters.GetValue("tipoId").Value<string>();
            string idCliente = parameters.GetValue("idCliente").Value<string>();
            string tarjetaAnterior = parameters.GetValue("tarjetaAnterior").Value<string>();
            string tarjetaNueva = parameters.GetValue("tarjetaNueva").Value<string>();
            string ip = parameters.GetValue("ip").Value<string>();

            JObject respesta = new JObject();
            if (!ValidaSiActivado(idCliente, tarjetaAnterior, tarjetaNueva))
            {
                try
                {
                    using (var httpClient = new HttpClient())
                    {
                        string baseUrlCms = configuration["Url_Services_CMS"];
                        string urlActivacion = configuration["URL_ACTIVACION_CMS"];
                        string url = string.Format("{0}{1}/{2}/{3}/{4}/{5}/{6}/{7}", baseUrlCms, urlActivacion, origen, tipoId, idCliente, tarjetaAnterior, tarjetaNueva, ip);
                        logger.Info<string>(string.Format("Request CMS {0}:", url));
                        Uri urlActivacionCms = new Uri(url);
                        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        var response = await httpClient.PostAsync(urlActivacionCms, new StringContent(""));
                        if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            var responseContent = await response.Content.ReadAsStringAsync();
                            var resultado = JsonConvert.DeserializeObject<JObject>(responseContent); logger.Info<string>(string.Format("Respuesta CMS {0}:", resultado.ToString(Formatting.None)));
                            var codigo = resultado.GetValue("codigo_Estado").Value<string>();
                            var descripcion = resultado.GetValue("descripcion_Estado").Value<string>();

                            respesta.Add("codigo", codigo);
                            respesta.Add("descripcion", descripcion);

                            HomologacionTarjetaLog logHomologacion = new HomologacionTarjetaLog();
                            logHomologacion.IdCliente = idCliente;
                            logHomologacion.TipoId = tipoId;
                            logHomologacion.TarjetaAnterior = tarjetaAnterior.Substring(12);
                            logHomologacion.TarjetaNueva = tarjetaNueva.Substring(12);
                            logHomologacion.AutorizaMaster = 1;
                            logHomologacion.EstadoRespuesta = codigo;
                            logHomologacion.Fecha = DateTime.Now.Date;
                            logHomologacion.Hora = DateTime.Now.TimeOfDay;
                            logHomologacion.Ip = ip;
                            if (codigo.Equals("000") || codigo.Equals("019"))
                            {
                                logHomologacion.Estado = "A";
                            }
                            else
                            {
                                logHomologacion.Estado = "M";
                            }
                            GuardarLog(logHomologacion);
                        }

                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex);
                }
            }
            else
            {
                respesta.Add("codigo", "501");
                respesta.Add("descripcion", "Estimado Cliente, su Tarjeta ya se encuentra activada y podra ser utilizada en una (1) hora");
            }
            return respesta;
        }

        private void GuardarLog(HomologacionTarjetaLog log)
        {
            try
            {
                Context.HomologacionTarjetaLogs.Add(log);
                Context.SaveChanges();
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }

        }

        private bool ValidaSiActivado(string idCLiente, string tarant, string tarnue)
        {

            bool yaValido = false;
            try
            {

                yaValido = Context.HomologacionTarjetaLogs.Any(x => x.IdCliente == idCLiente && x.TarjetaAnterior == tarant &&
                                                                    x.TarjetaNueva == tarnue && x.EstadoRespuesta == "000"
                                                                    && x.EstadoRespuesta == "019");

            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }

            return yaValido;
        }
    }
}
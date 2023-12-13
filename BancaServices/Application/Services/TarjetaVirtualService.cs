using Newtonsoft.Json.Linq;
using NLog;
using System.Data;
using IBM.Data.DB2.Core;
using BancaServices.Application.Services.SerfiUtils;
using BancaServices.Models.TarjetaVirtual;
using BancaServices.Domain.Interfaces;

namespace BancaServices.Application.Services
{
    public class TarjetaVirtualService : ITarjetaVirtualService
    {

        private readonly NLog.ILogger logger = LogManager.GetCurrentClassLogger();
        private CryptoUtil Util;
        private readonly IConfiguration _configuration;
        private readonly BancaServicesLogsEntities Context; // Inject the DbContext

        public TarjetaVirtualService(IConfiguration configuration, BancaServicesLogsEntities dbContext)
        {
            _configuration = configuration;
            Util = new CryptoUtil(configuration);
            Context = dbContext; // Assign the injected DbContext
        }


        public async Task<JObject> RegistrarLog(TarjetaVirtualLog log, string NumTarjeta)
        {
            JObject response = new JObject();

            log.Fecha = DateTime.Now.Date;
            log.Hora = DateTime.Now.TimeOfDay;
            JObject dataCard = new JObject();

            try
            {
                if (log.Respuesta == "ACEPTADO" || log.Respuesta == "RECHAZADO")
                {

                    Context.TarjetaVirtualLogs.Add(log);
                    await Context.SaveChangesAsync();

                }
                if (log.Respuesta == "ACEPTADO" || log.Respuesta == "CONSULTA")
                {
                    NovedadesBloqueoSR.NovedadesNoMon_WSClient client = new NovedadesBloqueoSR.NovedadesNoMon_WSClient();
                    NovedadesBloqueoSR.NovedadNoMon_Dto novedad = new NovedadesBloqueoSR.NovedadNoMon_Dto();
                    novedad.codigoCanal = "0214";
                    novedad.codigoEntidad = "0423";
                    novedad.tipoNovedad = "15";
                    novedad.nit = "860043186";
                    novedad.numeroTarjetaAsignado = Util.EncryptData(NumTarjeta);
                    novedad.tipoIdentificacion = log.TipoIdCliente;
                    novedad.numeroIdentificacion = Util.EncryptData(log.IdCliente);
                    string usuario = Util.EncryptData(_configuration["usuarioAutorizdor"]);
                    string pass = Util.EncryptData(_configuration["claveAutorizador"]);
                    logger.Info<NovedadesBloqueoSR.NovedadNoMon_Dto>(novedad);
                    var respuestaNovedad = client.aplicarNovedadAsync(novedad, usuario, pass).Result;
                    dataCard.Add("cvv", Util.DecryptData(respuestaNovedad.Body.aplicarNovedadReturn.filler));
                    dataCard.Add("fechaVencimiento", Util.DecryptData(respuestaNovedad.Body.aplicarNovedadReturn.fechaSolicitud));
                }

                response.Add("Codigo", "00");
                response.Add("Descripcion", "Registro exitoso.");
                response.Add("Respuesta", dataCard);
            }
            catch (Exception ex)
            {
                logger.Error("Error RegistrarLog:  Request={Request} Error={Error}", log, ex);
                response.Add("Codigo", "01");
                response.Add("Descripcion", "Ocurrio un error al registrar su respuesta.");
            }
            return response;
        }

        public async Task<TarjetaVirtualLog> ConsultarLog(TarjetaVirtualRequest data)
        {
            TarjetaVirtualLog response = new TarjetaVirtualLog();
            try
            {

                List<TarjetaVirtualLog> respuesta = Context.TarjetaVirtualLogs.Where(x => x.TipoIdCliente == data.TipoIdCliente && x.IdCliente == data.IdCliente).OrderByDescending(x => x.Id).ToList();
                if (respuesta.FirstOrDefault() != null)
                {
                    response = respuesta.FirstOrDefault();
                }

            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
            return response;
        }

        public async Task<JObject> ConsultaTarjetaVirtual(TarjetaVirtualRequest data)
        {
            JObject response = new JObject();
            try
            {
                string Estado = GetParamByDescription("StatusCodesVirtualCard");
                string[] CodProductos = GetParamByDescription("CodesVirtualCard").Split(',');
                var parametros = new List<string>();
                for (int i = 0; i < CodProductos.Length; i++)
                {
                    var parametro = "@codProducto" + i;
                    parametros.Add(parametro);
                }
                string[] MerProductos = GetParamByDescription("MarketCodesVirtualCard").Split(',');
                var mercados = new List<string>();
                for (int i = 0; i < MerProductos.Length; i++)
                {
                    var mercado = "@codMercado" + i;
                    mercados.Add(mercado);
                }

                string connString = _configuration.GetConnectionString("FACT");
                string query = string.Format("SELECT H3UNNB, H3NRTA, H3F3VA, JRLPCD FROM PHYESAT, PHYSOLI WHERE H3CDTP IN({0}) AND H3UENB = @estado AND H3UNNB = @idCliente AND H3CDTI = @tipoIdCliente AND JRLPCD IN({1}) AND JRU8NB = H3NRTA", string.Join(",", parametros), string.Join(",", mercados));
                using (DB2Connection conn = new DB2Connection(connString))
                {
                    conn.Open();

                    using (DB2Command command = new DB2Command(query, conn))
                    {
                        command.CommandType = CommandType.Text;
                        for (int i = 0; i < parametros.Count; i++)
                        {
                            command.Parameters.Add(new DB2Parameter(parametros[i], DB2Type.VarChar));
                            command.Parameters[parametros[i]].Value = CodProductos[i];
                        }
                        for (int i = 0; i < mercados.Count; i++)
                        {
                            command.Parameters.Add(new DB2Parameter(mercados[i], DB2Type.VarChar));
                            command.Parameters[mercados[i]].Value = MerProductos[i];
                        }
                        command.Parameters.Add(new DB2Parameter("@estado", DB2Type.VarChar));
                        command.Parameters["@estado"].Value = Estado;
                        command.Parameters.Add(new DB2Parameter("@idCliente", DB2Type.VarChar));
                        command.Parameters["@idCliente"].Value = data.IdCliente;
                        command.Parameters.Add(new DB2Parameter("@tipoIdCliente", DB2Type.VarChar));
                        command.Parameters["@tipoIdCliente"].Value = data.TipoIdCliente;

                        command.CommandType = CommandType.Text;
                        var reader = command.ExecuteReader();
                        JObject row = new JObject();
                        JArray ListaTarjetas = new JArray();

                        List<string> tarjetasCliente = new List<string>();
                        if (reader.HasRows)
                        {
                            tarjetasCliente = BuscarTarjetasClientes(data.TipoIdCliente, data.IdCliente);
                        }

                        while (reader.Read())
                        {
                            if (tarjetasCliente.Contains(reader["H3NRTA"].ToString()))
                            {
                                row["NumTarjeta"] = reader["H3NRTA"].ToString();
                                row["Cupo"] = reader["H3F3VA"].ToString();
                                row["Mercado"] = reader["JRLPCD"].ToString();
                                ListaTarjetas.Add(row);
                            }
                        }

                        reader.Close();
                        response.Add("Codigo", "00");
                        response.Add("Descripcion", "Consulta exitosa");
                        response.Add("Respuesta", row);
                        if (ListaTarjetas.Count > 1)
                        {
                            response.Add("RespuestaTarjetas", ListaTarjetas);
                        }

                    }
                }

            }
            catch (Exception ex)
            {
                logger.Error("Error ConsultaTarjetaVirtual:  Request={Request} Error={Error}", data, ex);
                response.Add("Codigo", "01");
                response.Add("Descripcion", "Ocurrio un error al consultar las tarjetas virtuales, " + ex.Message);
                response.Add("Respuesta", new JObject());
            }
            return response;
        }

        private string GetParamByDescription(string descripcion)
        {
            string response;
            try
            {
                var result = Context.Parametros.Where(x => x.DescripcionParametro == descripcion);
                response = result.FirstOrDefault().ValorParametro;

            }
            catch (Exception ex)
            {
                response = "";
            }
            return response;
        }

        private List<string> BuscarTarjetasClientes(string tipoId, string idCliente)
        {
            List<string> tarjetas = new List<string>();
            string codigoCanalConsultaTD = "194";
            string usuario = _configuration["usuarioAutorizdor"];
            string pass = _configuration["claveAutorizador"];
            ConsultaTarjetaSR.ConsultaTarjetasClient consultaCliente = new ConsultaTarjetaSR.ConsultaTarjetasClient();
            ConsultaTarjetaSR.ConsultaTarjetas_req request = new ConsultaTarjetaSR.ConsultaTarjetas_req();
            request.codigoCanal = codigoCanalConsultaTD;
            request.codigoEntidad = "0423";
            request.numeroDocumento = idCliente;
            request.tipoDocumento = tipoId;

            try
            {
                consultaCliente.InnerChannel.OperationTimeout = new TimeSpan(0, 0, 30);
                var tarjetaResponse = consultaCliente.getTarjetasDocumento(usuario, pass, request);

                if (tarjetaResponse.codigoRespuesta.Equals("OK000"))
                {
                    var tarjetasResponse = tarjetaResponse.arrayConsultaTarjetas;
                    foreach (var tarjeta in tarjetasResponse)
                    {
                        tarjetas.Add(tarjeta.numeroTarjetaCompleto);
                    }
                }
            }
            catch (TimeoutException timeEx)
            {
                Console.WriteLine(timeEx.Data);
            }

            return tarjetas;
        }
    }
}
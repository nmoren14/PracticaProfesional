using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using System.Data.Entity;
using System.Net;
using IBM.Data.DB2.Core;
using BancaServices.Models.RenovacionTC;
using BancaServices.Domain.Interfaces;
using BancaServices;

namespace BancaServices.Application.Services
{
    public class RenovacionTCServices : IRenovacionTCServices
    {
        private readonly IClienteServices _clienteService;
        private readonly IProductosService _productoService;
        private readonly NLog.ILogger logger = LogManager.GetCurrentClassLogger();
        private readonly INotificacionesServices _notificaciones;
        private readonly IConfiguration _configuration;
        private readonly BancaServicesLogsEntities Context; // Inject the DbContext


        public RenovacionTCServices(IConfiguration configuration, BancaServicesLogsEntities dbContext, IClienteServices clienteServices, IProductosService productosService, INotificacionesServices notificacionesServices)
        {
            _clienteService = clienteServices;
            _productoService = productosService;
            _notificaciones = notificacionesServices;
            _configuration = configuration;
            Context = dbContext; // Assign the injected DbContext
        }

        public async Task<List<TarjetaModelItem>> ConsultarRenovacion(string tipoId, string idCliente)
        {
            List<TarjetaModelItem> tarjetasResponse = new List<TarjetaModelItem>();
            try
            {
                JArray resCMS = await _productoService.ConsultaCMS(tipoId, idCliente, true);

                if (resCMS != null && resCMS.Any())
                {
                    string productosResponse = JsonConvert.SerializeObject(resCMS);

                    List<TarjetaModel> tarjetas = JsonConvert.DeserializeObject<List<TarjetaModel>>(productosResponse);
                    string sql = "SELECT TARNTA, H3FFVE, H3UENB TARIND FROM PHSREMTA, PHYESAT WHERE TARIND = '0' AND TARNTA IN (";
                    List<string> tarjetasJoin = new List<string>();
                    foreach (var item in tarjetas)
                    {
                        if (!string.IsNullOrEmpty(item.numProducto))
                        {
                            tarjetasJoin.Add("'" + item.numProducto + "'");
                        }

                    }
                    var arrayTarjetas = tarjetasJoin.ToArray();
                    string whereSql = string.Join(",", arrayTarjetas);
                    sql = sql + whereSql + ") AND TARNTA = H3NRTA";
                    tarjetasResponse = await ConsultarRenovacion(sql);
                }
            }
            catch (Exception ex)
            {
                logger.Error("Error ConsultarRenovacion TipoId={TipoId} IdCliente={IdCliente} {Error}", tipoId, idCliente, ex.ToString());
                return new List<TarjetaModelItem>();
            }
            return tarjetasResponse;
        }
        public async Task<List<TarjetaRenovada>> ConsultarEstadosTarjetasRenovacion(string tipoId, string idCliente)
        {
            List<TarjetaRenovada> tarjetasResponse = new List<TarjetaRenovada>();
            try
            {
                string productosResponse = JsonConvert.SerializeObject(await _productoService.ConsultaCMS(tipoId, idCliente, true));
                List<TarjetaModel> tarjetas = JsonConvert.DeserializeObject<List<TarjetaModel>>(productosResponse);
                string sql = "SELECT TARNTA, TARFEM, TARIND, TARCAN FROM PHSREMTA WHERE TARNTA IN (";
                List<string> tarjetasJoin = new List<string>();
                foreach (var item in tarjetas)
                {
                    if (!string.IsNullOrEmpty(item.numProducto))
                    {
                        tarjetasJoin.Add("'" + item.numProducto + "'");
                    }

                }
                var arrayTarjetas = tarjetasJoin.ToArray();
                string whereSql = string.Join(",", arrayTarjetas);
                sql = sql + whereSql + ") ";
                tarjetasResponse = await ConsultarEstadoTarjetaRenovacion(sql);
            }
            catch (Exception ex)
            {
                return new List<TarjetaRenovada>();
                logger.Error(ex);
            }
            return tarjetasResponse;
        }

        public async Task<RespuestaRenovarTC> RenovarTC(RequestRenovarTC request)
        {
            RespuestaRenovarTC result = new RespuestaRenovarTC();
            JObject log = new JObject();
            string Celular = string.Empty;
            try
            {
                log.Add("Request", JObject.FromObject(request));
                log.Add("ip", request.ip);
                result.Codigo = "01";
                result.Descripcion = "No se ha podido renovar tc";

                DateTime fecha = DateTime.Now;
                string productosResponse = JsonConvert.SerializeObject(await _productoService.ConsultaCMS(request.tipoId, request.idCliente, true));
                TarjetaModel tarjetaCliente = JsonConvert.DeserializeObject<List<TarjetaModel>>(productosResponse).Where(a => a.numProducto == request.tarjeta).FirstOrDefault();
                var tarjetasList = await ConsultarRenovacion(request.tipoId, request.idCliente);
                var tarjeta = tarjetasList.Where(a => a.Tarjeta == request.tarjeta).FirstOrDefault();
                if (tarjetaCliente != null && tarjeta != null)
                {
                    var respuesta = await RenovarAutorizador(request.tipoId, request.idCliente, request.tarjeta, tarjeta.FechaVencimiento, tarjeta.EstadoTarjeta);
                    if (respuesta)
                    {
                        string sql = string.Format("UPDATE PHSREMTA SET TARUSC = 'SER01PRY', TARCAN = '{0}', TARIND = '1', TARFEM = {1}, TARHOM = {2} WHERE TARNTA = '{3}'",
                                                    request.canal, fecha.ToString("yyyyMMdd"), fecha.ToString("HHmmss"), request.tarjeta);
                        int registros = await ActualizarRenovacion(sql);
                        if (registros > 0)
                        {
                            Celular = await ConsultarTelefono(request.tarjeta);
                            if (!string.IsNullOrEmpty(Celular))
                            {
                                if (Celular.Length <= 10)
                                {
                                    string mensaje = await GetMensaje();
                                    await _notificaciones.EnviarSmsAsync(Celular, string.Format(mensaje));
                                }
                            }
                            result.Codigo = "00";
                            result.Descripcion = "Exitoso";
                        }
                    }
                }
                log.Add("Respuesta", JObject.FromObject(result));
                log.Add("Celular", Celular);
                await GuardarLog("Result: " + JsonConvert.SerializeObject(log));
            }
            catch (Exception ex)
            {
                result.Codigo = "01";
                result.Descripcion = "Error: " + ex.Message;
                logger.Error(ex);
                log = new JObject();
                log.Add("Request", JObject.FromObject(request));
                log.Add("ip", request.ip);
                log.Add("Error", ex.ToString());
                await GuardarLog(JsonConvert.SerializeObject(result));
            }
            return result;
        }

        private async Task<string> GetMensaje()
        {
            try
            {
                var registro = await Context.Parametros.Where(a => a.DescripcionParametro == "TextoSMSRenovacion").FirstOrDefaultAsync();
                return registro.ValorParametro;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return "";
            }
        }

        public async Task GuardarLog(string stringlog)
        {
            try
            {
                RenovacionTC log = new RenovacionTC()
                {
                    Fecha = DateTime.Now,
                    Result = stringlog
                };
                Context.RenovacionTCs.Add(log);
                await Context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
        }

        private async Task<bool> RenovarAutorizador(string tipoId, string idCliente, string tarjeta, string FechaVencimiento, string EstadoVector)
        {
            try
            {
                NovedadesBloqueoSR.NovedadesNoMon_WSClient client = new NovedadesBloqueoSR.NovedadesNoMon_WSClient();
                NovedadesBloqueoSR.NovedadNoMon_Dto novedad = new NovedadesBloqueoSR.NovedadNoMon_Dto();
                novedad.codigoCanal = "0194";
                novedad.codigoEntidad = "0423";
                novedad.bin = tarjeta.Substring(0, 6);
                novedad.tipoNovedad = "16";
                novedad.numeroTarjetaAsignado = tarjeta;
                novedad.tipoIdentificacion = tipoId;
                novedad.numeroIdentificacion = idCliente;
                novedad.tipoCuenta = "31";
                novedad.filler = FechaVencimiento;
                string usuario = _configuration["usuarioAutorizdor"];
                string pass = _configuration["claveAutorizador"];

                LogEventInfo log = new LogEventInfo();
                log.LoggerName = "Autorizador Renovacion";
                log.Properties.Add("Request", JObject.FromObject(novedad).ToString());
                var respuestaNovedad = await client.aplicarNovedadAsync(novedad, usuario, pass);
                if (respuestaNovedad != null)
                {
                    log.Properties.Add("Response Renovacion", respuestaNovedad.Body.aplicarNovedadReturn.codigoRespuesta + " - " + respuestaNovedad.Body.aplicarNovedadReturn.descripcionRespuesta);
                    if (respuestaNovedad.Body.aplicarNovedadReturn.codigoRespuesta.Equals("OK000"))
                    {
                        novedad.tipoNovedad = "05";
                        novedad.nit = "860043186";
                        if (long.Parse(EstadoVector) == 10)
                        {
                            if (await ActualizarEstadoCMS("0", tarjeta) > 0)
                            {

                                var respuestaBloqueo = await client.aplicarNovedadAsync(novedad, usuario, pass);
                                if (respuestaBloqueo != null)
                                {
                                    log.Properties.Add("Actualizacion CMS", true);
                                    log.Properties.Add("Request Desbloqueo", JObject.FromObject(novedad).ToString());
                                    log.Properties.Add("Response Desbloqueo", respuestaBloqueo.Body.aplicarNovedadReturn.codigoRespuesta + " - " + respuestaBloqueo.Body.aplicarNovedadReturn.descripcionRespuesta);
                                    if (respuestaBloqueo.Body.aplicarNovedadReturn.codigoRespuesta.Equals("OK000"))
                                    {
                                        logger.Info(log);
                                        return true;
                                    }
                                }
                            }
                            else
                            {
                                log.Properties.Add("Actualizacion CMS", false);
                            }
                        }
                        else
                        {
                            var estadoCMS = EstadoVector.PadLeft(10, '0').ToCharArray();
                            if (estadoCMS[8].ToString() == "1")
                            {
                                estadoCMS[8] = '0';
                                var estadoString = new string(estadoCMS);
                                if (await ActualizarEstadoCMS(estadoString, tarjeta) > 0)
                                {
                                    log.Properties.Add("Actualizacion CMS", true);
                                    logger.Info(log);
                                    return true;
                                }
                                else
                                {
                                    log.Properties.Add("Actualizacion CMS", false);
                                }
                            }
                            else
                            {
                                log.Properties.Add("Actualizacion CMS no necesario", false);
                                logger.Info(log);
                                return true;
                            }
                        }
                    }
                }
                logger.Info(log);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
            return false;
        }

        private async Task<int> ActualizarEstadoCMS(string estadoVector, string tarjeta)
        {
            int actualizado = 0;
            string query = string.Empty;
            try
            {
                query = $"UPDATE PHYESAT SET H3UENB = '{estadoVector}' WHERE H3NRTA = '{tarjeta}'";
                string ConString = _configuration.GetConnectionString("FACT");
                using (DB2Connection connection = new DB2Connection(ConString))
                {
                    DB2Command command = new DB2Command();
                    try
                    {
                        connection.Open();
                        command.Connection = connection;
                        command.CommandText = query;
                        command.CommandTimeout = 10;
                        actualizado = await command.ExecuteNonQueryAsync();
                        //Si estado queda normal inserta en phynove
                        if (estadoVector == "0")
                        {
                            DateTime thisDate = DateTime.Now;
                            string HoraFormat = thisDate.ToString("HHmmss");
                            query = string.Format(@"INSERT INTO PHYNOVE (HCTPNB, HCBJNB, HCFNCD, HCTQNB, HCJCCD, HCUDNB, HCS4NB, HCL6TX, HCBKNB,HCKXTX, HCQ9NB, HCRANB, HCCDOF, HCJLT1) 
                                                VALUES ('{0}', {2}, '510', '8', '01', {2},{3}, 'SERVICE', {2}, 'D', {1}, {1}-10, '0001', '08001')", tarjeta, "0", thisDate.ToString("yyyyMMdd"), HoraFormat);
                            command.CommandText = query;
                            await command.ExecuteNonQueryAsync();
                        }
                        connection.Close();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                        logger.Error(e);
                    }
                }

            }
            catch (Exception ex)
            {
                throw;
            }
            return actualizado;
        }

        private async Task<int> insertPhyNove(string tarjeta)
        {
            DateTime thisDate = DateTime.Now;
            string HoraFormat = thisDate.ToString("HHmmss");
            int actualizado = 0;
            string query = string.Empty;
            try
            {
                query = string.Format(@"INSERT INTO PHYNOVE (HCTPNB, HCBJNB, HCFNCD, HCTQNB, HCJCCD, HCUDNB, HCS4NB, HCL6TX, HCBKNB,HCKXTX, HCQ9NB, HCRANB, HCCDOF, HCJLT1) 
                                                VALUES ('{0}', {2}, '510', '8', '01', {2},{3}, 'SERVICE', {2}, 'D', {1}, {1}-10, '0001', '08001')", tarjeta, "0", thisDate.ToString("yyyyMMdd"), HoraFormat);
                string ConString = _configuration.GetConnectionString("FACT");
                using (DB2Connection connection = new DB2Connection(ConString))
                {
                    DB2Command command = new DB2Command();
                    try
                    {
                        connection.Open();
                        command.Connection = connection;
                        command.CommandText = query;
                        command.CommandTimeout = 10;
                        actualizado = await command.ExecuteNonQueryAsync();
                        connection.Close();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                        logger.Error(e);
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
            return actualizado;
        }


        private async Task<int> ActualizarRenovacion(string query)
        {
            int actualizado = 0;
            try
            {
                string ConString = _configuration.GetConnectionString("FACT");
                using (DB2Connection connection = new DB2Connection(ConString))
                {
                    DB2Command command = new DB2Command();
                    try
                    {
                        connection.Open();
                        command.Connection = connection;
                        command.CommandText = query;
                        command.CommandTimeout = 10;
                        actualizado = await command.ExecuteNonQueryAsync();
                        connection.Close();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                        logger.Error(e);
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
            return actualizado;
        }

        private async Task<List<TarjetaModelItem>> ConsultarRenovacion(string query)
        {
            List<TarjetaModelItem> tarjetas = new List<TarjetaModelItem>();
            try
            {
                string ConString = _configuration.GetConnectionString("FACT");
                using (DB2Connection connection = new DB2Connection(ConString))
                {
                    DB2Command command = new DB2Command();
                    try
                    {
                        connection.Open();
                        command.Connection = connection;
                        command.CommandText = query;
                        var reader = await command.ExecuteReaderAsync();
                        while (reader.Read())
                        {
                            if (!reader.IsDBNull(0))
                            {
                                tarjetas.Add(new TarjetaModelItem() { Tarjeta = reader.GetValue(0).ToString(), FechaVencimiento = reader.GetValue(1).ToString().Substring(2), EstadoTarjeta = reader.GetValue(2).ToString() });
                            }
                        }
                        connection.Close();
                    }
                    catch (Exception e)
                    {
                        logger.Error("Error ConsultarRenovacion Query {Query} {Error}", query, e.ToString());
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            return tarjetas;
        }

        private async Task<List<TarjetaRenovada>> ConsultarEstadoTarjetaRenovacion(string query)
        {
            List<TarjetaRenovada> tarjetas = new List<TarjetaRenovada>();
            try
            {
                string ConString = _configuration.GetConnectionString("FACT");
                using (DB2Connection connection = new DB2Connection(ConString))
                {
                    DB2Command command = new DB2Command();
                    try
                    {
                        connection.Open();
                        command.Connection = connection;
                        command.CommandText = query;
                        var reader = await command.ExecuteReaderAsync();
                        while (reader.Read())
                        {
                            TarjetaRenovada tarjetaRenovada = new TarjetaRenovada();

                            tarjetaRenovada.Tarjeta = reader.GetValue(0).ToString().Substring(reader.GetValue(0).ToString().Length - 4);
                            string fechaActivacion = reader.GetValue(1).ToString();
                            if (reader.GetValue(2).ToString() == "1")
                            {
                                DateTime condate = DateTime.ParseExact(fechaActivacion, "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
                                string strDate = condate.ToString("dd/MM/yyyy");
                                tarjetaRenovada.FechaActivacion = strDate;
                                tarjetaRenovada.MedioRenovacion = reader.GetValue(3).ToString() ?? "";
                            }
                            else
                            {
                                tarjetaRenovada.FechaActivacion = "";
                                tarjetaRenovada.MedioRenovacion = "";
                            }
                            tarjetaRenovada.EstadoTarjeta = reader.GetValue(2).ToString().Equals("0") ? "PENDIENTE RENOVAR" : "RENOVADA EFECTUADA";
                            tarjetas.Add(tarjetaRenovada);

                        }
                        connection.Close();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                        logger.Error(e);
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            return tarjetas;
        }

        private async Task<List<RenovacionTarjeta>> ConsultarTarjetaRenovacion(string query)
        {
            List<RenovacionTarjeta> tarjetas = new List<RenovacionTarjeta>();
            try
            {
                string ConString = _configuration.GetConnectionString("FACT");
                using (DB2Connection connection = new DB2Connection(ConString))
                {
                    DB2Command command = new DB2Command();
                    try
                    {
                        connection.Open();
                        command.Connection = connection;
                        command.CommandText = query;
                        var reader = await command.ExecuteReaderAsync();
                        while (reader.Read())
                        {
                            if (!reader.IsDBNull(0))
                            {
                                tarjetas.Add(new RenovacionTarjeta() { Tarjeta = reader.GetValue(0).ToString(), FechaVencimiento = reader.GetValue(1).ToString().Substring(2), EstadoTarjeta = reader.GetValue(2).ToString(), EstadoRenovacion = reader.GetValue(3).ToString(), Cedula = reader.GetValue(4).ToString().Trim() });
                            }
                        }
                        connection.Close();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                        logger.Error(e);
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            return tarjetas;
        }
        public async Task<RespuestaGenerarOTP> GenerarOTP(string tipoId, string idCliente, string tarjeta)
        {
            RespuestaGenerarOTP respuesta = new RespuestaGenerarOTP();
            try
            {
                string telefono = string.Empty;
                telefono = await ConsultarTelefono(tarjeta);
                var (mensaje, pin) = await EnviarPIN(telefono, tipoId, idCliente);
                if (mensaje)
                {
                    respuesta.Codigo = "00";
                    respuesta.PIN = pin;
                    respuesta.Telefono = telefono;
                }
                else
                {
                    respuesta.Codigo = "01";
                    respuesta.PIN = "";
                    respuesta.Telefono = "";
                }
            }
            catch (Exception ex)
            {
                respuesta.Codigo = "01";
                respuesta.PIN = "";
                respuesta.Telefono = "";
                logger.Error(ex);
            }
            return respuesta;
        }

        private async Task<(bool, string)> EnviarPIN(string telefono, string tipoid, string idcliente)
        {
            bool exitoso = false;
            string pin = string.Empty;
            try
            {
                var urlbus = _configuration["BUS_REST_ADAPTER"].ToString();
                DateTime fecha = DateTime.Now;
                var json = "{\"requestHeaderOut\":{\"Header\":{\"systemId\":\"Helpi\",\"messageId\":\"xxfech1xxx\",\"invokerDateTime\":\"xxfech2xxx\",\"securityCredential\":\"\",\"destination\":{\"name\":\"SeguridadApiGroup\",\"namespace\":\"http://www.serfinansa.co/seguridad/external/1.0\",\"operation\":\"generaPIN\"}},\"Body\":{\"generaPIN\":{\"tipoId\":\"xxtidxx\",\"idCliente\":\"xidx\",\"celular\":\"xxcelxx\"}}}}".Replace("xxcelxx", telefono).Replace("xidx", idcliente).Replace("xxtidxx", tipoid).Replace("xxfech1xxx", fecha.ToString("yyyyMMddHHmmssfff")).Replace("xxfech2xxx", fecha.ToString("yyyy-MM-dd"));
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(urlbus);
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";
                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    streamWriter.Write(json);
                    streamWriter.Flush();
                    streamWriter.Close();
                }
                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                if (httpResponse.StatusDescription == "OK")
                {
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                    {
                        try
                        {
                            var result = JObject.Parse(streamReader.ReadToEnd());
                            if (result != null)
                            {
                                string codigo = result.SelectToken("responseHeaderOut.Body.respuesta.codigo").Value<string>();
                                if (codigo.Equals("00"))
                                {
                                    exitoso = true;
                                    pin = result.SelectToken("responseHeaderOut.Body.respuesta.pin").Value<string>();
                                }
                            }
                        }
                        catch (Exception)
                        {

                        }
                    }
                }
            }
            catch (Exception)
            {

            }
            return (exitoso, pin);
        }

        private async Task<string> ConsultarTelefono(string numeroTarjeta)
        {
            string telefono = string.Empty;
            try
            {
                string ConString = _configuration.GetConnectionString("FACT");
                using (DB2Connection connection = new DB2Connection(ConString))
                {
                    DB2Command command = new DB2Command();
                    try
                    {
                        connection.Open();
                        command.Connection = connection;
                        command.CommandText = string.Format("SELECT H3T1NB FROM PHYESAT WHERE H3NRTA = '{0}'", numeroTarjeta);
                        var reader = await command.ExecuteReaderAsync();
                        while (reader.Read())
                        {
                            if (!reader.IsDBNull(0))
                            {
                                telefono = reader.GetValue(0).ToString();
                            }
                        }
                        connection.Close();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                        logger.Error(e);
                    }
                }
            }
            catch (Exception)
            {
                return "";
            }
            return telefono;
        }

        public async Task<RespuestaCargueMasivo> RenovacionMasiva(int renovacionCargueId, int RenovacionLogId)
        {
            RespuestaCargueMasivo response = new RespuestaCargueMasivo();
            JObject request = new JObject();
            request.Add("renovacionCargueId", renovacionCargueId);
            request.Add("RenovacionLogId", RenovacionLogId);
            string celular = string.Empty;
            try
            {
                var validateRenovacionModel = await Context.RenovacionCargues.Where(x => x.RenovacionCargueId == renovacionCargueId).FirstOrDefaultAsync();
                var validateRenovacionLog = await Context.RenovacionCargueLogs.Where(x => x.RenovacionCargueId == RenovacionLogId).FirstOrDefaultAsync();
                if (validateRenovacionLog != null && validateRenovacionModel != null)
                {
                    var cuerpoCargueList = await Context.CuerpoRenovacionCargues.Where(x => x.RenovacionCargueId == renovacionCargueId).ToListAsync();
                    if (cuerpoCargueList.Count > 0)
                    {
                        var listGroup = cuerpoCargueList.GroupBy(x => x.Cedula).Select(x => new Tuple<string, string>(x.Key, x.Select(c => c.TipoDocumento).First())).ToList();
                        var tarjetaModelItems = await ConsultarTarjetadeRenovacion(listGroup);
                        foreach (var item in cuerpoCargueList)
                        {
                            var tarjeta = tarjetaModelItems.Where(x => x.Tarjeta.Substring(x.Tarjeta.Length - 4) == item.Tarjeta && x.Cedula == item.Cedula).FirstOrDefault();
                            if (tarjeta != null)
                            {
                                if (tarjeta.EstadoRenovacion == "0")
                                {
                                    DateTime fecha = DateTime.Now;
                                    var respuesta = await RenovarAutorizador(item.TipoDocumento, item.Cedula, tarjeta.Tarjeta, tarjeta.FechaVencimiento, tarjeta.EstadoTarjeta);
                                    if (respuesta)
                                    {
                                        string sql = string.Format("UPDATE PHSREMTA SET TARUSC = 'SER01PRY', TARCAN = '{0}', TARIND = '1', TARFEM = {1}, TARHOM = {2} WHERE TARNTA = '{3}'",
                                                                    "HEOP", fecha.ToString("yyyyMMdd"), fecha.ToString("HHmmss"), tarjeta.Tarjeta);
                                        int registros = await ActualizarRenovacion(sql);
                                        if (registros > 0)
                                        {
                                            item.Renovado = true;
                                            item.FechaRenovacion = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                                            item.Respuesta = "Renovacion exitosa";


                                        }
                                        else
                                        {
                                            item.Respuesta = "Renovacion no exitosa";
                                            item.DetalleError = "Error al actualizar la renovación";
                                            item.Renovado = false;
                                        }
                                    }
                                    else
                                    {
                                        item.Respuesta = "Renovacion no exitosa";
                                        item.DetalleError = "Error del autorizador";
                                        item.Renovado = false;
                                    }
                                }
                                else
                                {
                                    item.Respuesta = "Numero de tarjeta fue renovada";
                                    item.Renovado = false;
                                    item.FechaRenovacion = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                                }
                            }
                            else
                            {
                                item.Respuesta = "Renovacion no exitosa";
                                item.DetalleError = "tarjeta no encontrada";
                            }
                        }
                        var listRenovados = cuerpoCargueList.Where(x => x.Renovado.Value).ToList();
                        if (listRenovados.Count > 0)
                        {
                            foreach (var item in listRenovados)
                            {
                                var producto = tarjetaModelItems.Where(x => x.Tarjeta.Substring(x.Tarjeta.Length - 4) == item.Tarjeta && x.Cedula == item.Cedula).FirstOrDefault();
                                celular = await ConsultarTelefono(producto.Tarjeta);
                                if (!string.IsNullOrEmpty(celular))
                                {
                                    if (celular.Length <= 10)
                                    {
                                        string mensaje = await GetMensaje();
                                        await _notificaciones.EnviarSmsAsync(celular, string.Format(mensaje));
                                    }
                                }
                            }

                        }
                        validateRenovacionModel.EstadoCargue = "FINALIZADO";
                        validateRenovacionModel.Renovadas = listRenovados.Count;
                        validateRenovacionModel.NoRenovadas = cuerpoCargueList.Where(x => !x.Renovado.Value).Count();
                        validateRenovacionLog.EstadoCargue = "FINALIZADO";
                        await Context.SaveChangesAsync();
                        response.Codigo = "00";
                        response.Descripcion = "Renovacion masiva finalizada";
                        response.EstadoCargue = true;


                    }
                    else
                    {
                        response.Codigo = "01";
                        response.Descripcion = "No se encontraron registros para renovar";
                        validateRenovacionModel.EstadoCargue = "ERROR";
                        validateRenovacionLog.EstadoCargue = "ERROR";
                        await Context.SaveChangesAsync();
                        return response;
                    }
                }
                else
                {
                    response.Codigo = "01";
                    response.Descripcion = "No existe un cargue de renovación con el identificador";
                    return response;
                }

            }
            catch (Exception ex)
            {
                response = new RespuestaCargueMasivo();
                response.Error = ex.ToString();
                saveLogMasive(nameof(RenovacionMasiva), JsonConvert.SerializeObject(request), ex.ToString());
            }

            return response;
        }

        public async Task<List<RenovacionTarjeta>> ConsultarTarjetadeRenovacion(List<Tuple<string, string>> cuerpo)
        {
            List<RenovacionTarjeta> tarjetasResponse = new List<RenovacionTarjeta>();
            string sql = string.Empty;
            try
            {
                foreach (var registro in cuerpo)
                {
                    string productosResponse = JsonConvert.SerializeObject(await _productoService.ConsultaCMS(registro.Item2, registro.Item1, true));
                    List<TarjetaModel> tarjetas = JsonConvert.DeserializeObject<List<TarjetaModel>>(productosResponse);
                    sql = "SELECT TARNTA, H3FFVE, H3UENB, TARIND, H3UNNB FROM PHSREMTA, PHYESAT WHERE TARNTA IN (";
                    List<string> tarjetasJoin = new List<string>();
                    foreach (var item in tarjetas)
                    {
                        if (!string.IsNullOrEmpty(item.numProducto))
                        {
                            tarjetasJoin.Add("'" + item.numProducto + "'");
                        }

                    }
                    var arrayTarjetas = tarjetasJoin.ToArray();
                    string whereSql = string.Join(",", arrayTarjetas);
                    sql = sql + whereSql + ") AND TARNTA = H3NRTA";
                    tarjetasResponse.AddRange(await ConsultarTarjetaRenovacion(sql));

                }


            }
            catch (Exception ex)
            {
                return new List<RenovacionTarjeta>();
                logger.Error(ex);
            }
            return tarjetasResponse;
        }
        private void saveLogMasive(string proceso, string request, string response)
        {

            RenovacionMasivoLog renovacion = new RenovacionMasivoLog()
            {
                Proceso = proceso,
                Request = request,
                Response = response,
                Fecha = DateTime.Now
            };
            Context.RenovacionMasivoLogs.Add(renovacion);
            Context.SaveChanges();

        }
    }
}
using Newtonsoft.Json.Linq;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Globalization;
using System.Net;
using IBM.Data.DB2.Core;
using BancaServices.Models;
using System.Text;
using BancaServices.Domain.Interfaces;
using BancaServices;

namespace BancaServices.Application.Services
{
    public class RediferirServicesV2 : IRediferirServicesV2
    {
        private readonly IConfiguration _configuration;
        private readonly BancaServicesLogsEntities Context; // Inject the DbContext

        public RediferirServicesV2(IConfiguration configuration, BancaServicesLogsEntities dbContext)
        {
            _configuration = configuration;
            Context = dbContext; // Assign the injected DbContext
        }
        public async Task<JObject> ValidarConsultaCMS(JArray listaCms, string TipoId, string IdCliente)
        {
            JObject respuesta = new JObject();
            try
            {
                JArray Contenido = await ConsultarContenido();
                var listaProductos = new JArray();
                bool hasError = false;
                StringBuilder mensaje = null;

                if (listaCms == null)
                {
                    mensaje = new StringBuilder();
                    mensaje.Append(listaCms == null ? string.Format("consulta de Tarjetas-CMS {0}", Environment.NewLine) : string.Empty);
                    hasError = true;
                }
                JObject referencias;
                JObject condiciones;
                if (listaCms != null && listaCms.Count > 0)
                {
                    foreach (JObject productoCms in listaCms)
                    {
                        int categoria = int.Parse(productoCms.GetValue("categoria").Value<string>());
                        if (categoria == 4/* || categoria == 2*/)
                        {
                            referencias = new JObject();
                            condiciones = new JObject();
                            if (categoria == 4)
                            {
                                condiciones = await ValidarCondiciones(productoCms.GetValue("numProducto").Value<string>(), TipoId, IdCliente, "04", new JObject());
                            }
                            else
                            {
                                //condiciones = await ValidarCondiciones(productoCms.GetValue("numProducto").Value<string>(), TipoId, IdCliente, productoCms.GetValue("categoria").Value<string>(), productoCms);
                            }

                            bool apto = condiciones.GetValue("Respuesta").Value<bool>();
                            if (apto)
                            {
                                referencias.Add("numProducto", productoCms.GetValue("numProducto").Value<string>());
                                referencias.Add("nomProducto", productoCms.GetValue("nomProducto").Value<string>());
                                referencias.Add("categoria", productoCms.GetValue("categoria").Value<string>());
                                referencias.Add("DiasMora", condiciones.GetValue("DiasMora").Value<string>());
                                referencias.Add("termCondiciones", condiciones.GetValue("TermCondiciones").Value<JArray>());
                                listaProductos.Add(referencias);
                            }
                        }
                    }
                    if (listaProductos.Count > 0)
                    {
                        respuesta.Add("descripcion", "Transacción exitosa");
                    }
                    else
                    {
                        respuesta.Add("codigo", "03");
                        respuesta.Add("descripcion", "Los productos del cliente no cumplen con los requisitos para Rediferir Cuotas");
                    }
                }
                else
                {
                    respuesta.Add("codigo", "03");
                    respuesta.Add("descripcion", "Cliente no tiene productos asociados para rediferir");
                }

                if (listaProductos.Count > 0)
                {
                    if (hasError)
                    {
                        respuesta.Add("codigo", "03");
                        respuesta.Add("descripcion", "Ocurrio un error al consultar los productos");
                    }
                    else
                    {
                        respuesta.Add("codigo", "00");
                        respuesta.Add("productos", listaProductos);
                        respuesta.Add("Contenido", Contenido);
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            return respuesta;
        }
        public async Task<JObject> RediferirCliente(JObject data)
        {
            JObject result = new JObject();
            RediferidosLog LogModel = new RediferidosLog();
            string monto = string.Empty;
            string plazo = string.Empty;
            try
            {
                string CardNumber = data.GetValue("CardNumber").Value<string>();
                result.Add("Codigo", "01");
                if (CardNumber.Trim().Length == 16)
                {
                    LogModel.TipoId = ConsultarTipoID(CardNumber);
                    LogModel.IdCliente = ConsultarCedula(CardNumber);
                    LogModel.Reestructurado = ConsultarReestructurado(CardNumber);
                    monto = decimal.Round(await ConsultarMonto(CardNumber), 0).ToString();
                    result.Add("CardNumber", CardNumber.Substring(12));
                    LogModel.NumTarjeta = CardNumber.Substring(12);
                    LogModel.Monto = monto;
                }
                else
                {
                    LogModel.Reestructurado = string.Empty;
                    ConsultarClienteResponse cliente = await EnviarRequest(CardNumber);
                    if (cliente != null)
                    {
                        if (cliente.estado)
                        {
                            LogModel.TipoId = ConsultarTipoDocumento(cliente.data.cuenta.tipoIdCliente);
                            LogModel.IdCliente = long.Parse(cliente.data.cuenta.idCliente);
                            LogModel.Monto = cliente.data.cuenta.pagoTotal.ToString();
                        }
                        else
                        {
                            result = new JObject();
                            result.Add("Codigo", "02");
                            result.Add("Descripcion", "Ha ocurrido un error al consultar los datos cliente");
                            return result;
                        }
                    }
                    else
                    {
                        result = new JObject();
                        result.Add("Codigo", "02");
                        result.Add("Descripcion", "Ha ocurrido un error al consultar los datos cliente");
                        return result;
                    }
                    result.Add("CardNumber", CardNumber.Substring(8));
                    LogModel.NumTarjeta = CardNumber.Substring(8);
                }
                plazo = data.GetValue("Plazo").Value<string>();
                string NuevaCuota = decimal.Round(await CalcularCuota(CardNumber, plazo), 0).ToString();
                result.Add("NuevaCuota", NuevaCuota);
                result.Add("Plazo", plazo);
                result.Add("Tasa", ConsultarTasa().ToString());
                //Model

                LogModel.IpCliente = data.GetValue("Ip").Value<string>();
                LogModel.FechaRegistro = DateTime.Now;
                LogModel.Plazo = int.Parse(plazo);
                LogModel.Categoria = data.GetValue("Categoria").Value<string>();
                LogModel.DiasMora = data.GetValue("DiasMora").Value<string>();
                result.Add("Descripcion", "Solicitud recibida, sujeta a estudio para aprobación.");
                LogModel.Respuesta = result.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                result = new JObject();
                result.Add("Codigo", "02");
                result.Add("Descripcion", "Error en la gestion " + ex.Message.ToString());
                LogModel.Respuesta = ex.ToString();
            }
            await GuardarTransaccion(LogModel);
            return result;
        }
        public async Task<JObject> CalcularCuotaPorTarjeta(JObject data)
        {
            JObject result = new JObject();
            string monto = "";
            string plazo = "";
            string tasa = "";
            try
            {
                string CardNumber = data.GetValue("CardNumber").Value<string>();
                monto = decimal.Round(await ConsultarMonto(CardNumber), 0).ToString();
                plazo = data.GetValue("Plazo").Value<string>();
                tasa = ConsultarTasa().ToString();
                string NuevaCuota = decimal.Round(await CalcularCuota(CardNumber, plazo), 0).ToString();
                result.Add("Codigo", "01");
                result.Add("Descripcion", "Exitoso");
                if (CardNumber.Length == 16)
                {
                    result.Add("CardNumber", CardNumber.Substring(12));
                }
                else
                {
                    result.Add("CardNumber", CardNumber.Substring(8));
                }
                result.Add("Monto", monto);
                result.Add("Plazo", plazo);
                result.Add("Tasa", tasa + "%");
                result.Add("NuevaCuota", NuevaCuota);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                result = new JObject();
                result.Add("Codigo", "02");
                result.Add("Descripcion", "Error en la gestion " + ex.Message.ToString());
            }
            return result;
        }
        public async Task<JObject> apps(string tipoId, string idcliente)
        {
            JObject result = new JObject();
            JObject Body = new JObject();
            string NameSpaceapp = "http://www.serfinansa.co/productos/external/2.0";
            string UrlproductosIBS = _configuration["BUS_REST_ADAPTER"];
            JObject Header = GenerarHeaderBus("ProductosApiGroup", NameSpaceapp, "apps");
            Body.Add("apps", new JObject() { { "tipoId", tipoId }, { "idCliente", idcliente } });

            JObject requestHeaderOut = new JObject();
            requestHeaderOut.Add("requestHeaderOut", new JObject() {
                {"Header", Header },
                { "Body", Body}
            });
            try
            {
                result = await EnviarPeticion(UrlproductosIBS, requestHeaderOut.ToString(), "");
                result = JObject.Parse(result["responseHeaderOut"]["Body"].ToString());
            }
            catch (Exception ex)
            {
                result = new JObject();
                result.Add("Codigo", "02");
                result.Add("Descripcion", "No se pudo consultar el resumen de productos");
                result.Add("Error", ex.Message);
                return result;
                throw;
            }
            return result;
        }

        #region MetodosDelServicio
        private int ConsultarTipoDocumento(string TipoDoc)
        {
            int Documento = 0;
            switch (TipoDoc.ToUpper())
            {
                case "CC":
                    Documento = 1;
                    break;
                case "CE":
                    Documento = 2;
                    break;
                case "NIT":
                    Documento = 3;
                    break;
                case "TI":
                    Documento = 4;
                    break;
                case "PAS":
                    Documento = 5;
                    break;
                case "CD":
                    Documento = 6;
                    break;
                case "EI":
                    Documento = 7;
                    break;
                case "RCN":
                    Documento = 8;
                    break;
                case "FID":
                    Documento = 9;
                    break;
            }
            return Documento;
        }
        public async Task<JObject> ValidarCondiciones(string CardNumber, string TipoId, string IdCliente, string Categoria, JObject producto)
        {
            JObject result = new JObject();
            bool cumple = true;
            decimal monto;
            DateTime FechaApertura;
            bool Bloqueado;
            decimal DiasMora;
            string Tasa;
            bool Solicitud;
            try
            {
                //Consultas respectivas
                if (Categoria.Equals("04"))
                {
                    monto = await ConsultarMonto(CardNumber);
                    FechaApertura = DateTime.ParseExact(ConsultarFechaApertura(CardNumber), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None);
                    //Bloqueado = ConsultarBloqueoTarjeta(CardNumber);
                    DiasMora = ConsultarMoraTarjeta(CardNumber, Categoria);
                    if (FechaApertura > DateTime.Now.AddMonths(-3)) { cumple = false; }
                    //if (Bloqueado) { cumple = false; }
                }
                else
                {
                    monto = await ConsultarMonto(CardNumber);
                    DiasMora = ConsultarMoraTarjeta(CardNumber, Categoria);
                }
                Tasa = ConsultarTasa().ToString();
                Solicitud = await ConsultarSolicitud(CardNumber, TipoId, IdCliente);
                JArray TermCondiciones = await ConsultarTerminosCondiciones(Categoria, DiasMora);
                //Validaciones
                if (monto < 50000) { cumple = false; }
                if (DiasMora > 60) { cumple = false; }
                if (Solicitud) { cumple = false; }
                if (cumple)
                {
                    result.Add("Codigo", "01");
                    result.Add("Descipcion", "El cliente con la tarjeta terminada en " + CardNumber.Substring(12) + " cumple con los requisitos para rediferir sus cuotas");
                    result.Add("Respuesta", true);
                    result.Add("Monto", monto.ToString());
                    result.Add("Tasa", Tasa + "%");
                    result.Add("DiasMora", DiasMora.ToString());
                    result.Add("TermCondiciones", TermCondiciones);
                    //result.Add("Contenido", Contenido);
                }
                else
                {
                    result.Add("Codigo", "01");
                    result.Add("Descipcion", "No cumple con los requisitos para rediferir sus cuotas");
                    result.Add("Respuesta", false);
                    result.Add("CardNumber", CardNumber.Substring(12));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                result = new JObject();
                result.Add("Codigo", "02");
                result.Add("Respuesta", false);
                result.Add("Descripcion", "Error en la gestion " + ex.Message.ToString());
            }
            return result;
        }
        private async Task<JArray> ConsultarContenido()
        {
            JArray AContenido = new JArray();
            try
            {

                List<TerminosYCondicionesRediferidos> ListContenido;

                ListContenido = await Context.TerminosYCondicionesRediferidos.Where(a => a.Contenido == true).OrderBy(a => a.GrupoCondicion).ToListAsync();
                JObject Contenido;
                foreach (TerminosYCondicionesRediferidos item in ListContenido)
                {
                    Contenido = new JObject();
                    Contenido.Add("NumContenido", item.NumeroCondicion);
                    Contenido.Add("NumGrupo", item.GrupoCondicion);
                    Contenido.Add("Descripcion", item.Descripcion.TrimEnd());
                    AContenido.Add(Contenido);
                }

            }
            catch (Exception)
            {
                throw;
            }
            return AContenido;
        }
        private async Task<JArray> ConsultarTerminosCondiciones(string Categoria, decimal DiasMora)
        {
            JArray termCondiciones = new JArray();
            int grupoCondiciones = 0;
            try
            {

                List<TerminosYCondicionesRediferidos> ListTerminos;
                if (Categoria.Equals("04"))
                {
                    //Olimpica menor y mayor a 30 dias de mora
                    grupoCondiciones = 1;
                }
                else if (Categoria.Equals("02"))
                {
                    if (DiasMora < 30)
                    {
                        //Consumo menor a 30 dias de mora
                        grupoCondiciones = 3;
                    }
                    else
                    {
                        //Consumo mayor a 30 dias de mora
                        grupoCondiciones = 4;
                    }
                }
                ListTerminos = await Context.TerminosYCondicionesRediferidos.Where(a => a.GrupoCondicion == grupoCondiciones).OrderBy(a => a.NumeroCondicion).OrderBy(a => a.GrupoCondicion).ToListAsync();
                JObject Condiciones;
                foreach (TerminosYCondicionesRediferidos item in ListTerminos)
                {
                    Condiciones = new JObject();
                    Condiciones.Add("NumCondicion", item.NumeroCondicion);
                    Condiciones.Add("Descripcion", item.Descripcion.TrimEnd());
                    termCondiciones.Add(Condiciones);
                }

            }
            catch (DbEntityValidationException dbEx)
            {
                foreach (var errors in dbEx.EntityValidationErrors)
                {

                }
            }

            return termCondiciones;
        }
        public async Task<decimal> CalcularCuota(string CardNumber, string Cplazo)
        {
            decimal cuota = 0;
            double monto = Convert.ToDouble(await ConsultarMonto(CardNumber));
            double tasa = ConsultarTasa();
            double plazo = int.Parse(Cplazo);
            try
            {
                //Calcular cuota
                tasa = tasa / 100;
                cuota = Convert.ToDecimal(monto * (tasa * Math.Pow(1 + tasa, plazo) / (Math.Pow(1 + tasa, plazo) - 1)));
            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return cuota;
        }
        private string ConsultarReestructurado(string CardNumber)
        {
            string Reestructurado = string.Empty;
            try
            {
                string ConString = _configuration.GetConnectionString("FACT");
                string query = GetQueryConsultarReestructurado(CardNumber);
                using (DB2Connection connection = new DB2Connection(ConString))
                {
                    DB2Command command = new DB2Command();
                    try
                    {
                        connection.Open();
                        command.Connection = connection;
                        command.CommandText = query;
                        DB2DataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            if (!reader.IsDBNull(0))
                            {
                                DateTime FechaReestructurado = DateTime.ParseExact(reader.GetString(0), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None);
                                if (FechaReestructurado > DateTime.Now.AddMonths(-6)) { Reestructurado = "PR"; }
                            }
                        }
                        connection.Close();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            return Reestructurado;
        }
        private async Task<bool> ConsultarSolicitud(string CardNumber, string TipoId, string IdCliente)
        {
            try
            {
                int Tipo_Id = int.Parse(TipoId);
                long Id_Cliente = long.Parse(IdCliente);
                string NumTarjeta = string.Empty;
                if (CardNumber.Trim().Length == 16)
                {
                    NumTarjeta = CardNumber.Substring(12);
                }
                else
                {
                    NumTarjeta = CardNumber.Substring(8);
                }
                int DiasEsperaSolicitud = int.Parse(_configuration["MesesEsperaSolicitarRediferido"]);

                List<RediferidosLog> redi = await Context.RediferidosLogs.Where(a => a.TipoId == Tipo_Id && a.IdCliente == Id_Cliente && a.NumTarjeta == NumTarjeta).ToListAsync();
                if (redi.Count > 0)
                {
                    DateTime TieneSolicitud = redi.Max(x => x.FechaRegistro).GetValueOrDefault();
                    if (TieneSolicitud.AddMonths(DiasEsperaSolicitud) > DateTime.Now)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }

            }
            catch (DbEntityValidationException dbEx)
            {
                foreach (var errors in dbEx.EntityValidationErrors)
                {

                }
            }
            return false;
        }
        private async Task GuardarTransaccion(RediferidosLog transaccion)
        {
            try
            {


                Context.RediferidosLogs.Add(transaccion);
                await Context.SaveChangesAsync();

            }
            catch (DbEntityValidationException dbEx)
            {
                foreach (var errors in dbEx.EntityValidationErrors)
                {

                }
            }
        }
        public static JObject GenerarHeaderBus(string NombreDestino, string NameSpace, string Operacion)
        {
            DateTime fecha = DateTime.Now;
            string messageId = fecha.ToString("yyyyMMddHHmmssffff");
            string invokerDateTime = fecha.ToString("yyyy-MM-dd");
            //DESTINATION VA DENTRO DE HEADER
            JObject destination = new JObject();
            destination.Add("name", NombreDestino);
            destination.Add("namespace", NameSpace);
            destination.Add("operation", Operacion);
            ///se arma el HEADER
            JObject Header = new JObject();
            Header.Add("systemId", "HelpiPlus");
            Header.Add("messageId", messageId);
            Header.Add("invokerDateTime", invokerDateTime);
            Header.Add("securityCredential", "");
            Header.Add("destination", destination);
            return Header;
        }
        public async Task<JObject> EnviarPeticion(string Url, string JsonString, string Authorization)
        {
            try
            {
                JObject result = new JObject();
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(Url);
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";
                httpWebRequest.Headers.Add(HttpRequestHeader.Authorization, Authorization);
                using (var streamWriter = new StreamWriter(await httpWebRequest.GetRequestStreamAsync()))
                {
                    //string postData = JsonString;
                    await streamWriter.WriteAsync(JsonString);
                    await streamWriter.FlushAsync();
                    streamWriter.Close();
                }
                var httpResponse = (HttpWebResponse)await httpWebRequest.GetResponseAsync();
                if (httpResponse.StatusDescription == "OK")
                {
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                    {
                        result = JObject.Parse(await streamReader.ReadToEndAsync());
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                JObject result = new JObject();
                result.Add("error", ex.Message);
                return result;
            }
        }
        private async Task<ConsultarClienteResponse> EnviarRequest(string CardNumber)
        {
            ConsultarClienteResponse respuesta;
            try
            {
                using (var httpClient = new HttpClient())
                {
                    Uri urlCashOut = new Uri(string.Format("{0}{1}", _configuration["URL_CONSULTAR_CLIENTE"], CardNumber));
                    var response = await httpClient.GetAsync(urlCashOut);
                    respuesta = await response.Content.ReadAsAsync<ConsultarClienteResponse>();
                    //respuesta = JObject.Parse(response.Content.ReadAsStringAsync().Result);
                }
            }
            catch (Exception ex)
            {
                return null;
            }
            return respuesta;
        }
        private string ConsultarFechaApertura(string CardNumber)
        {
            string FechaApertura = "";
            try
            {
                string ConString = _configuration.GetConnectionString("FACT");
                string query = GetQueryConsultarFechaApertura(CardNumber);
                using (DB2Connection connection = new DB2Connection(ConString))
                {
                    DB2Command command = new DB2Command();
                    try
                    {
                        connection.Open();
                        command.Connection = connection;
                        command.CommandText = query;
                        DB2DataReader reader = command.ExecuteReader();

                        while (reader.Read())
                        {
                            FechaApertura = reader.GetString(0).ToString();
                        }
                        connection.Close();
                    }

                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return FechaApertura;
        }
        private double ConsultarTasa()
        {
            double TasaMes = 0;
            try
            {

                TasaVigente tasa = new TasaVigente();
                tasa = Context.TasaVigentes.Where(a => a.Año == DateTime.Now.Year && a.Mes == DateTime.Now.Month).FirstOrDefault();
                TasaMes = Convert.ToDouble(tasa.Tasa.ToString());

            }
            catch (Exception)
            {
                throw;
            }
            return TasaMes;
        }
        private async Task<decimal> ConsultarMonto(string CardNumber)
        {
            decimal monto = 0;
            try
            {
                string ConString = string.Empty;
                string query = string.Empty;
                if (CardNumber.Trim().Length == 16)
                {
                    ConString = _configuration.GetConnectionString("FACT");
                    query = GetQueryConsultarMonto(CardNumber, 1);
                }
                else
                {
                    ConsultarClienteResponse cliente = await EnviarRequest(CardNumber);
                    if (cliente != null)
                    {
                        if (cliente.estado)
                        {
                            monto = Convert.ToDecimal(cliente.data.cuenta.pagoTotal);
                            return monto;
                        }
                        else
                        {
                            return 0;
                        }
                    }
                    else
                    {
                        return 0;
                    }
                    //ConString = ConfigurationManager.ConnectionStrings["SEFYLES"].ConnectionString;
                    //query = GetQueryConsultarMonto(CardNumber, 2);
                }
                using (DB2Connection connection = new DB2Connection(ConString))
                {
                    DB2Command command = new DB2Command();
                    try
                    {
                        connection.Open();
                        command.Connection = connection;
                        command.CommandText = query;
                        DB2DataReader reader = command.ExecuteReader();

                        while (reader.Read())
                        {
                            monto = decimal.Parse(reader.GetString(0));
                        }
                        reader.Close();
                        connection.Close();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return monto;
        }
        private bool ConsultarBloqueoTarjeta(string CardNumber)
        {
            bool Bloqueado = true;
            try
            {
                string ConString = _configuration.GetConnectionString("FACT");
                string query = GetQueryConsultarBloqueo(CardNumber);
                string Estado = "";
                using (DB2Connection connection = new DB2Connection(ConString))
                {
                    DB2Command command = new DB2Command();
                    try
                    {
                        connection.Open();
                        command.Connection = connection;
                        command.CommandText = query;
                        DB2DataReader reader = command.ExecuteReader();

                        while (reader.Read())
                        {
                            Estado = reader.GetString(0);
                        }
                        connection.Close();
                        if (Estado.Equals("0")) { Bloqueado = false; }
                    }

                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return Bloqueado;
        }
        private decimal ConsultarMoraTarjeta(string CardNumber, string Categoria)
        {
            decimal DiasMora = 0;
            try
            {
                string ConString = string.Empty;
                string query = string.Empty;
                if (Categoria.Equals("04"))
                {
                    ConString = _configuration.GetConnectionString("FACT");
                    string Referencia = ConsultarReferencia(CardNumber);
                    query = GetQueryConsultarDiasMora(Referencia, Categoria);
                }
                else if (Categoria.Equals("02"))
                {
                    ConString = _configuration.GetConnectionString("SEFYLES");
                    query = GetQueryConsultarDiasMora(CardNumber, Categoria);
                }
                using (DB2Connection connection = new DB2Connection(ConString))
                {
                    DB2Command command = new DB2Command();
                    try
                    {
                        connection.Open();
                        command.Connection = connection;
                        command.CommandText = query;
                        DB2DataReader reader = command.ExecuteReader();

                        while (reader.Read())
                        {
                            DiasMora = decimal.Parse(reader.GetString(0));
                        }
                        connection.Close();
                    }

                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return DiasMora;
        }
        private string ConsultarReferencia(string CardNumber)
        {
            string Referencia = string.Empty;
            try
            {
                string ConString = _configuration.GetConnectionString("FACT");
                string query = GetQueryConsultarReferencia(CardNumber);
                using (DB2Connection connection = new DB2Connection(ConString))
                {
                    DB2Command command = new DB2Command();
                    try
                    {
                        connection.Open();
                        command.Connection = connection;
                        command.CommandText = query;
                        DB2DataReader reader = command.ExecuteReader();

                        while (reader.Read())
                        {
                            Referencia = reader.GetString(0).ToString();
                        }
                        connection.Close();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
            return Referencia;
        }
        private long ConsultarCedula(string CardNumber)
        {
            long Cedula = 0;

            try
            {
                string ConString = _configuration.GetConnectionString("FACT");
                string query = GetQueryConsultarCedula(CardNumber);
                using (DB2Connection connection = new DB2Connection(ConString))
                {
                    DB2Command command = new DB2Command();
                    try
                    {
                        connection.Open();
                        command.Connection = connection;
                        command.CommandText = query;
                        DB2DataReader reader = command.ExecuteReader();

                        while (reader.Read())
                        {
                            Cedula = int.Parse(reader.GetString(0));
                        }
                        connection.Close();
                    }

                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }

            return Cedula;
        }
        private int ConsultarTipoID(string CardNumber)
        {
            int TipoId = 0;
            try
            {
                string ConString = _configuration.GetConnectionString("FACT");
                string query = GetQueryConsultarTipoID(CardNumber);
                using (DB2Connection connection = new DB2Connection(ConString))
                {
                    DB2Command command = new DB2Command();
                    try
                    {
                        connection.Open();
                        command.Connection = connection;
                        command.CommandText = query;
                        DB2DataReader reader = command.ExecuteReader();

                        while (reader.Read())
                        {
                            TipoId = int.Parse(reader.GetString(0));
                        }
                        connection.Close();
                    }

                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
            return TipoId;
        }
        #endregion

        #region QuerysDelServicio
        private string GetQueryConsultarReferencia(string Card)
        {
            return string.Format("SELECT C1CTAN FROM PHSCTAA WHERE C1CTA = '{0}'", Card);
        }
        private string GetQueryConsultarReestructurado(string Card)
        {
            return string.Format("SELECT MAX(C5FFPL) FROM PHYHTRA WHERE C5NRTA='{0}' AND C5CDTT = 'PR'", Card);
        }
        private string GetQueryConsultarDiasMora(string Referencia, string Categoria)
        {
            if (Categoria.Equals("04"))
            {
                return string.Format("SELECT IBDIMORA FROM PHSINCIIHC WHERE IBFECCOR = '202002' AND IBNMREF = '{0}'", Referencia);
            }
            else
            {
                return string.Format("SELECT CPVMOR FROM CPVPV WHERE CPVDTY = '2020' AND CPVDTM = '2' AND CPVACC = {0}", Referencia);
            }
        }
        private string GetQueryConsultarTipoID(string Card)
        {
            return string.Format("SELECT H3CDTI FROM PHYESAT WHERE H3NRTA = '{0}'", Card);
        }
        private string GetQueryConsultarBloqueo(string Card)
        {
            return string.Format("SELECT H3UENB FROM PHYESAT WHERE H3NRTA = '{0}'", Card);
        }
        private string GetQueryConsultarMonto(string Card, int opc)
        {
            if (opc == 1)
            {
                return string.Format("SELECT ((SELECT COALESCE(SUM(DYCXPM),0) FROM PHYESLD WHERE DYNRTA = '{0}') +  H3R6VA) AS MONTO FROM PHYESAT WHERE H3NRTA = '{0}'", Card);
            }
            else
            {
                return string.Format("SELECT DEAMEP FROM DEALS WHERE DEAACC = {0}", Card);
            }
        }
        private string GetQueryConsultarFechaApertura(string Card)
        {
            return string.Format("SELECT H3MQNB FROM PHYESAT WHERE H3NRTA = '{0}'", Card);
        }
        private string GetQueryConsultarCedula(string Card)
        {
            return string.Format("SELECT H3UNNB FROM PHYESAT WHERE H3NRTA = '{0}'", Card);
        }
        #endregion
    }
}
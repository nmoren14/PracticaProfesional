using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using System.Data.Entity.Validation;
using System.Data.SqlClient;
using System.Net.Http.Headers;
using BancaServices.Models.BLoqueoProducto;
using BancaServices.Models;
using BancaServices.Domain.Interfaces;
using BancaServices.Application.Services.SerfiUtils;

namespace BancaServices.Application.Services
{
    public class BloqueoServices : IBloqueoServices
    {
        private readonly NLog.ILogger logger = LogManager.GetCurrentClassLogger();
        private readonly BancaServicesLogsEntities Context; // Inject the DbContext
        private const string CODIGO_ERROR_CMS = "100OK";
        private readonly IConfiguration _configuration;
        public BloqueoServices(IConfiguration configuration, BancaServicesLogsEntities dbContext)
        {
            _configuration = configuration;
            Context = dbContext; // Assign the injected DbContext
        }

        public JObject BloquearTCO(JObject parameters)
        {
            var restpuesta = new JObject();
            string tarjeta = string.Empty;
            string tipoId;
            string idCliente;
            try

            {
                try
                {
                    tipoId = parameters.GetValue("tipoId").Value<string>();
                    idCliente = parameters.GetValue("idCliente").Value<string>();

                    if (parameters.ContainsKey("numProducto"))
                    {
                        tarjeta = parameters.GetValue("numProducto").Value<string>();
                    }
                    else if (parameters.ContainsKey("numTarjeta"))
                    {
                        tarjeta = parameters.GetValue("numTarjeta").Value<string>();
                    }

                }
                catch (Exception)
                {
                    restpuesta.Add("codigo", "001");
                    restpuesta.Add("descripcion", "error en parametros");
                    return restpuesta;
                }


                (bool bloqueadoCms, string DescripcionCms) = BloquearTCOCMS(tipoId, idCliente, tarjeta);
                (bool bloqueadoAutorizador, string DescripcionAutorizador) = BloquearAutorizador(tipoId, idCliente, tarjeta);

                if (bloqueadoAutorizador && bloqueadoCms)
                {
                    restpuesta.Add("codigo", "000");
                    restpuesta.Add("descripcion", "Transacción exitosa");
                }
                else
                {
                    restpuesta.Add("codigo", "001");
                    restpuesta.Add("descripcion", "Se presento un error al bloquear el producto");

                    try
                    {

                        string Asunto = "Servicio de bloqueo de TCO";
                        string Body = $"Ocurrio un error al bloquear la tarjeta terminada en {tarjeta.Substring(12)} del cliente con documento No. {idCliente} DetalleCms={DescripcionCms} DetalleAutorizador={DescripcionAutorizador}";

                        NotificarErrorUtil.NotificarError(Asunto, Body, _configuration);
                    }
                    catch (Exception ex)
                    {
                        logger.Error<Exception>("Error Bloquear NotificarError Error={Error}", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error<Exception>("Error BloquearTCO Error={Error}", ex);
            }

            return restpuesta;
        }

        private (bool, string) BloquearAutorizador(string tipoId, string idCliente, string tarjeta)
        {
            string Descripcion = string.Empty;
            try
            {
                NovedadesBloqueoSR.NovedadesNoMon_WSClient client = new NovedadesBloqueoSR.NovedadesNoMon_WSClient();
                NovedadesBloqueoSR.NovedadNoMon_Dto novedad = new NovedadesBloqueoSR.NovedadNoMon_Dto();
                novedad.codigoCanal = "0194";
                novedad.codigoEntidad = "0423";
                novedad.bin = tarjeta.Substring(0, 6);
                novedad.tipoNovedad = "04";
                novedad.numeroTarjetaAsignado = tarjeta;
                novedad.tipoIdentificacion = tipoId;
                novedad.numeroIdentificacion = idCliente;
                novedad.motivoBloqueo = "4";
                novedad.tipoCuenta = "31";
                string usuario = _configuration["usuarioAutorizdor"];
                string pass = _configuration["claveAutorizador"];

                logger.Info<NovedadesBloqueoSR.NovedadNoMon_Dto>("Request BloquearAutorizador={novedad}", novedad);

                var respuestaNovedad = client.aplicarNovedadAsync(novedad, usuario, pass).Result;
                if (respuestaNovedad != null)
                {
                    logger.Info<NovedadesBloqueoSR.aplicarNovedadResponse>("Respuesta BloquearAutorizador={respuestaNovedad}", respuestaNovedad);

                    if (respuestaNovedad.Body != null && !string.IsNullOrEmpty(respuestaNovedad.Body.aplicarNovedadReturn.descripcionRespuesta) && !string.IsNullOrEmpty(respuestaNovedad.Body.aplicarNovedadReturn.descripcionRespuesta.Trim()))
                    {
                        Descripcion = respuestaNovedad.Body.aplicarNovedadReturn.descripcionRespuesta;
                    }
                    else
                    {
                        Descripcion = JsonConvert.SerializeObject(respuestaNovedad, Formatting.None);
                        logger.Info("Respuesta BloquearAutorizador: {0}", Descripcion);
                    }

                    if (respuestaNovedad.Body.aplicarNovedadReturn.codigoRespuesta.Equals("OK000"))
                    {
                        return (true, Descripcion);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error<Exception>("Error BloquearAutorizador Error={Error}", ex);
            }

            return (false, Descripcion);
        }

        private (bool, string) BloquearTCOCMS(string tipoId, string idCliente, string tarjeta)
        {

            string Descripcion = string.Empty;
            JObject resultado = new JObject();
            try
            {

                using (var httpClient = new HttpClient())
                {
                    string baseUrlCms = _configuration["Url_Services_CMS"];
                    string urlBloqueoCms = _configuration["Bloqueo_Producto_CMS"];

                    string url = string.Format("{0}{1}/{2}/{3}/{4}/{5}/{6}", baseUrlCms, urlBloqueoCms, idCliente, tipoId, "CN002", tarjeta.Substring(12), "10.231.30.54");
                    logger.Info<string>(string.Format("Bloque CMS URL:{0}", url));
                    Uri urlResumenCms = new Uri(url);
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    var response = httpClient.GetAsync(urlResumenCms).Result;
                    if ((int)response.StatusCode == 500)
                    {
                        Descripcion = $"{(int)response.StatusCode}-{response.StatusCode.ToString()}";
                    }
                    else
                    {
                        //resultado = response.Content.ReadAsAsync<JObject>().Result;
                        Descripcion = resultado.ToString(Formatting.None);
                        string vError = string.Empty;
                        if (resultado.ContainsKey("vERROR"))
                        {
                            vError = resultado.GetValue("vERROR").Value<string>();
                        }

                        if (resultado.ContainsKey("VERROR"))
                        {
                            vError = resultado.GetValue("VERROR").Value<string>();
                        }

                        if (vError.Equals(CODIGO_ERROR_CMS))
                        {
                            return (true, Descripcion);
                        }
                    }

                    logger.Info("Respuesta bloqueo CMS: {tipoId} {IdCliente} {Respuesta}", tipoId, idCliente, Descripcion);




                }
            }
            catch (Exception ex)
            {
                logger.Error<Exception>("Error Bloquear BloquearTCOCMS Error={Error}", ex);
            }

            return (false, Descripcion);
        }

        public async Task<JObject> BloquearUsuario(JObject data)
        {
            JObject result = new JObject();
            UsuariosBloqueadosLog LogModel = new UsuariosBloqueadosLog();
            try
            {
                string TipoId = data.GetValue("TipoId").Value<string>();
                string Documento = data.GetValue("Documento").Value<string>();
                bool succes = await Bloquear(TipoId, Documento);
                LogModel = new UsuariosBloqueadosLog()
                {
                    Documento = Documento,
                    TipoId = int.Parse(TipoId)
                };
                if (succes)
                {
                    result.Add("Codigo", "01");
                    result.Add("Descripcion", "Exitoso");
                    LogModel.Result = result.GetValue("Descripcion").Value<string>();
                    GuardarRegistro(LogModel);
                }
                else
                {
                    result.Add("Codigo", "02");
                    result.Add("Descripcion", "No se encontro el usuario con los parametros enviados");
                    LogModel.Result = result.ToString();
                    GuardarRegistro(LogModel);
                }
            }
            catch (Exception ex)
            {
                result.Add("Codigo", "03");
                result.Add("Descripcion", "Error");
                result.Add("Error", ex.Message);
                LogModel.Result = result.ToString();
                LogModel.Error = ex.Message;
                GuardarRegistro(LogModel);
            }
            return result;
        }
        private void GuardarRegistro(UsuariosBloqueadosLog transaccion)
        {
            try
            {
                Context.UsuariosBloqueadosLogs.Add(transaccion);
                Context.SaveChanges();

            }
            catch (DbEntityValidationException dbEx)
            {
                foreach (var errors in dbEx.EntityValidationErrors)
                {

                }
            }
        }
        private async Task<bool> Bloquear(string TipoId, string Documento)
        {
            bool OK = false;
            string IdUser = string.Empty;
            int update = 0;
            try
            {
                string sql = GetQueryConsultarIdUserName(TipoId, Documento);
                string connStr = _configuration["InterPersonal"].ToString();
                SqlConnection conn = new SqlConnection(connStr);
                conn.Open();
                SqlCommand cmd = new SqlCommand()
                {
                    CommandText = sql,
                    Connection = conn
                };
                SqlDataReader reader = await cmd.ExecuteReaderAsync();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        IdUser = reader.GetInt32(0).ToString();
                    }
                    reader.Close();
                    sql = getQueryBloquearUsuario(IdUser);
                    cmd.CommandText = sql;
                    update = await cmd.ExecuteNonQueryAsync();
                }
                conn.Close();
                if (update > 0) { OK = true; }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return OK;
        }
        #region Scripts
        private string getQueryBloquearUsuario(string CustomerId)
        {
            string estado = _configuration["EstadoBloqueo"];
            return string.Format("UPDATE [CORE].[USER] SET USERSTATEID = '{0}' WHERE CUSTOMERID = '{1}'", estado, CustomerId);
        }
        private string GetQueryConsultarIdUserName(string TipoId, string Documento)
        {
            return string.Format("SELECT [CustomerId] FROM [BdIntPersonal].[Bank].[Customer] WHERE IDENTIFICATIONTYPEID = '{0}' AND IDENTIFICATION = '{1}'", TipoId, Documento);
        }
        #endregion

        public async Task<JObject> BloquearProductos(BloquearProducto producto)
        {

            JObject response = new JObject();
            string procedure = producto.tipoBloqueo.Equals("01") ? "Bloquear Usuario" : producto.tipoBloqueo.Equals("02") ? "Bloquear Cuenta" : "Bloquear Cuenta y Bloquear usuario";
            try
            {
                switch (producto.productoBloqueo)
                {
                    //Bloquea el producto enviado
                    case "U":

                        List<CreditCardBin> creditCardBins = Context.CreditCardBins.ToList();
                        if (producto.tipoBloqueo.Equals("02") || producto.tipoBloqueo.Equals("03"))
                        {
                            if (producto.tipoProducto.Equals("04"))
                            {
                                if (creditCardBins.Where(x => x.IdentityCardBin.Equals(producto.numProducto.Substring(0, 6))).Any())
                                {
                                    response = await TipoBloqueo(producto);
                                }
                                else
                                {
                                    response.Add("codigo", "001");
                                    response.Add("descripcion", "El numero del producto no coincide con el bin");
                                }
                            }
                            else
                            {
                                response = await TipoBloqueo(producto);
                            }
                        }
                        else
                        {
                            response = await TipoBloqueo(producto);
                        }
                        SaveBloquearProductosLogs(producto, response, procedure);
                        break;

                    //Bloquea todos los productos 
                    case "T":
                        response = await tipoProducto(producto);
                        break;
                }

            }
            catch (Exception ex)
            {
                response = new JObject();
                response.Add("codigo", "002");
                response.Add("descripcion", "Error : " + ex.Message);
                SaveBloquearProductosLogs(producto, response, "Error");
            }

            return response;
        }
        #region Tipo de Bloqueo
        public async Task<JObject> TipoBloqueo(BloquearProducto producto)
        {
            JObject response = new JObject();
            JObject data = new JObject();
            try
            {
                string codeBlockUser = string.Empty;
                string codeBlockProduct = string.Empty;
                JObject bloquearTCO = new JObject();
                JObject bloquearUsuario = new JObject();
                data.Add("TipoId", producto.tipoId);
                data.Add("tipoId", producto.tipoId);
                data.Add("Documento", producto.idCliente);
                data.Add("idCliente", producto.idCliente);
                data.Add("numProducto", producto.numProducto);
                switch (producto.tipoBloqueo)
                {
                    case "01":
                        response = new JObject();
                        bloquearUsuario = await BloquearUsuario(data);
                        codeBlockUser = bloquearUsuario.GetValue("Codigo").Value<string>();
                        if (codeBlockUser.Equals("01"))
                        {
                            response.Add("codigo", "000");
                            response.Add("descripcion", "Transacción existosa");
                        }
                        else if (codeBlockUser.Equals("02"))
                        {
                            response.Add("codigo", "001");
                            response.Add("descripcion", "No se encontro el usuario con los parametros enviados");
                        }
                        else
                        {
                            response.Add("codigo", "002");
                            response.Add("descripcion", "Ha ocurrido un error, intente mas tarde");
                        }

                        break;
                    case "02":

                        response = new JObject();

                        bloquearTCO = BloquearTCO(data);
                        codeBlockProduct = bloquearTCO.GetValue("codigo").Value<string>();
                        if (codeBlockProduct.Equals("000"))
                        {
                            response.Add("codigo", "000");
                            response.Add("descripcion", "Transacción exitosa");
                        }
                        else if (codeBlockProduct.Equals("001"))
                        {
                            response.Add("codigo", "001");
                            response.Add("descripcion", bloquearTCO["descripcion"].ToString());
                        }
                        else
                        {
                            response.Add("codigo", "002");
                            response.Add("descripcion", "Ha ocurrido un error, intente mas tarde");
                        }



                        break;
                    case "03":
                        response = new JObject();

                        bloquearTCO = BloquearTCO(data);
                        bloquearUsuario = await BloquearUsuario(data);
                        codeBlockUser = bloquearUsuario.GetValue("Codigo").Value<string>();
                        codeBlockProduct = bloquearTCO.GetValue("codigo").Value<string>();
                        if (codeBlockProduct.Equals("000") && codeBlockUser.Equals("01"))
                        {
                            response.Add("codigo", "000");
                            response.Add("descripcion", "Transacción exitosa");
                        }
                        else if (codeBlockUser.Equals("02") && codeBlockProduct.Equals("001"))
                        {
                            response.Add("codigo", "001");
                            response.Add("descripcion", "Se presento un error al bloquear el producto");
                        }
                        else
                        {
                            response.Add("codigo", "002");
                            response.Add("descripcion", "Ha ocurrido un error, intente mas tarde");
                        }


                        break;
                }
            }
            catch (Exception ex)
            {

                response = new JObject();
                response.Add("codigo", "002");
                response.Add("descripcion", "Error : " + ex.Message);
            }
            return response;
        }
        #endregion
        #region Guardar logs de productos bloqueados
        public void SaveBloquearProductosLogs(BloquearProducto producto, JObject response, string procedure)
        {
            try
            {
                string request = JsonConvert.SerializeObject(producto);
                BloquearProductosLog bloquearProductosLog = new BloquearProductosLog()
                {
                    Request = request,
                    TypeCard = producto.tipoProducto,
                    Response = response.ToString(),
                    Action = procedure,
                    Date = DateTime.Now.ToString()
                };

                Context.BloquearProductosLogs.Add(bloquearProductosLog);
                Context.SaveChanges();

            }
            catch (Exception ex)
            {
                logger.Error<string>("Error al guardar en SaveBloquearProductosLogs" + ex.ToString());
                throw ex;

            }
        }
        #endregion

        private List<product> fillProducts(Dictionary<string, string> data, string tipoProducto)
        {
            List<product> products = new List<product>();
            List<CreditCardBin> creditCardBins = Context.CreditCardBins.ToList();

            try
            {
                foreach (var item in data)
                {
                    if (tipoProducto.Equals("05"))
                    {
                        if (item.Value.Equals(ClaveParametros.TARJETA_DEBITO))
                        {
                            product product = new product();
                            product.numProducto = item.Key;
                            products.Add(product);
                        }

                    }
                    else
                    {
                        if (item.Value.Equals(ClaveParametros.TARJETA_CREDITO))
                        {
                            if (creditCardBins.Where(x => x.IdentityCardBin.Equals(item.Key.Substring(0, 6))).Any())
                            {
                                product product = new product();
                                product.numProducto = item.Key;
                                products.Add(product);
                            }
                        }

                    }

                }
            }
            catch (Exception)
            {
                products = null;
            }

            return products;
        }
        public async Task<JObject> tipoProducto(BloquearProducto producto)
        {
            JObject response = new JObject();
            BloquearProducto copyProduct = producto;
            string codeBlockUser = string.Empty;
            string codeBlockProduct = string.Empty;
            string procedure = producto.tipoBloqueo.Equals("01") ? "Bloquear Usuario" : producto.tipoBloqueo.Equals("02") ? "Bloquear Cuenta" : "Bloquear Cuenta y Bloquear usuario";
            try
            {

                JObject data = new JObject();
                data.Add("TipoId", producto.tipoId);
                data.Add("tipoId", producto.tipoId);
                data.Add("Documento", producto.idCliente);
                data.Add("idCliente", producto.idCliente);
                data.Add("numProducto", producto.numProducto);
                string changeId = Utils.HomologarTipoId(producto.tipoId, Utils.Sistema.AUTO);
                Dictionary<string, string> cards = BuscarTarjetasClientes(changeId, producto.idCliente);
                List<product> products = new List<product>();
                if (cards != null)
                {
                    products = fillProducts(cards, producto.tipoProducto);
                    if (products != null)
                    {
                        if (products.Count > 0)
                        {
                            JArray ListProductsLog = new JArray();
                            JObject log = new JObject();
                            if (producto.tipoBloqueo.Equals("03"))
                            {
                                JObject bloquearUsuario = new JObject();
                                bloquearUsuario = await BloquearUsuario(data);
                                foreach (var item in products)
                                {
                                    JObject bloquearTCO = new JObject();
                                    data["numProducto"] = item.numProducto;
                                    bloquearTCO = BloquearTCO(data);
                                    codeBlockProduct = bloquearTCO.GetValue("codigo").Value<string>();
                                    bloquearTCO.Add("numProducto", item.numProducto);
                                    ListProductsLog.Add(bloquearTCO);
                                }
                                codeBlockUser = bloquearUsuario.GetValue("Codigo").Value<string>();
                                log.Add("bloquearCuentas", ListProductsLog);
                                log.Add("bloquearUsuario", bloquearUsuario);
                                foreach (var item in ListProductsLog)
                                {
                                    if (item["codigo"].ToString().Equals("001"))
                                    {
                                        codeBlockProduct = item["codigo"].ToString();
                                        break;
                                    }
                                }

                                if (codeBlockProduct.Equals("000") && codeBlockUser.Equals("01"))
                                {
                                    response.Add("codigo", "000");
                                    response.Add("descripcion", "Transacción exitosa");
                                }
                                else if (codeBlockUser.Equals("02") && codeBlockProduct.Equals("001"))
                                {
                                    response.Add("codigo", "001");
                                    response.Add("descripcion", "Se presento un error al bloquear el producto");
                                }
                                else
                                {
                                    response.Add("codigo", "002");
                                    response.Add("descripcion", "Ha ocurrido un error, intente mas tarde");
                                }
                                SaveBloquearProductosLogs(copyProduct, log, procedure);
                            }
                            else if (producto.tipoBloqueo.Equals("02"))
                            {
                                foreach (var item in products)
                                {

                                    producto.numProducto = item.numProducto;
                                    JObject bloqueo = new JObject();
                                    bloqueo = await TipoBloqueo(producto);
                                    bloqueo.Add("numProducto", item.numProducto);
                                    ListProductsLog.Add(bloqueo);
                                }
                                foreach (var item in ListProductsLog)
                                {
                                    codeBlockProduct = item["codigo"].ToString();
                                    if (item["codigo"].ToString().Equals("001"))
                                    {
                                        codeBlockProduct = item["codigo"].ToString();
                                        break;
                                    }
                                }
                                if (codeBlockProduct.Equals("000"))
                                {
                                    response.Add("codigo", "000");
                                    response.Add("descripcion", "Transacción exitosa");
                                }
                                else if (codeBlockProduct.Equals("001"))
                                {
                                    response.Add("codigo", "001");
                                    response.Add("descripcion", "Se presento un error al bloquear el producto");
                                }
                                else
                                {
                                    response.Add("codigo", "002");
                                    response.Add("descripcion", "Ha ocurrido un error, intente mas tarde");
                                }

                                log.Add("detallesBloqueo", ListProductsLog);
                                SaveBloquearProductosLogs(copyProduct, log, procedure);
                            }
                            else
                            {
                                response = await TipoBloqueo(producto);
                                SaveBloquearProductosLogs(copyProduct, response, procedure);
                            }
                        }
                        else
                        {
                            response = new JObject();
                            response.Add("codigo", "001");
                            response.Add("descripcion", "No se encontraron tarjetas activas o no coinciden con el bin");
                            SaveBloquearProductosLogs(copyProduct, response, procedure);
                        }
                    }
                    else
                    {
                        response = new JObject();
                        response.Add("codigo", "001");
                        response.Add("descripcion", "No se encontraron tarjetas para este cliente");
                        SaveBloquearProductosLogs(copyProduct, response, procedure);
                    }
                }
                else
                {
                    response = new JObject();
                    response.Add("codigo", "001");
                    response.Add("descripcion", "No se encontraron productos asociados");
                    SaveBloquearProductosLogs(copyProduct, response, procedure);
                }


            }
            catch (Exception ex)
            {
                response = new JObject();
                response.Add("codigo", "002");
                response.Add("descripcion", "Error : " + ex.Message);
            }
            return response;
        }
        public Dictionary<string, string> BuscarTarjetasClientes(string tipoId, string idCliente)
        {
            Dictionary<string, string> tarjetas = new Dictionary<string, string>();
            string codigoCanalConsultaTD = _configuration["CODIGO_CANAL_CONSULTA_TD"];//213 encriptado
            string usuario = string.Empty;
            string pass = string.Empty;
            ConsultaTarjetaSR.ConsultaTarjetasClient consultaCliente = new ConsultaTarjetaSR.ConsultaTarjetasClient();
            ConsultaTarjetaSR.ConsultaTarjetas_req request = new ConsultaTarjetaSR.ConsultaTarjetas_req();
            request.codigoCanal = codigoCanalConsultaTD;
            request.codigoEntidad = "0423";
            request.numeroDocumento = idCliente;
            request.tipoDocumento = tipoId;

            //Se valida si el se debe encriptar o no
            if (codigoCanalConsultaTD.Equals("194"))
            {
                request.numeroDocumento = idCliente;
                usuario = _configuration["usuarioAutorizdor"];
                pass = _configuration["claveAutorizador"];
            }
            else
            {
                CryptoUtil cryptoUtil = new CryptoUtil(_configuration);
                request.numeroDocumento = cryptoUtil.EncryptData(idCliente);
                usuario = cryptoUtil.EncryptData(_configuration["usuarioAutorizdor"]);
                pass = cryptoUtil.EncryptData(_configuration["claveAutorizador"]);
            }


            try
            {
                consultaCliente.InnerChannel.OperationTimeout = new TimeSpan(0, 0, 30);
                var tarjetaResponse = consultaCliente.getTarjetasDocumento(usuario, pass, request);

                if (tarjetaResponse.codigoRespuesta.Equals("OK000"))
                {
                    var tarjetasResponse = tarjetaResponse.arrayConsultaTarjetas;
                    foreach (var tarjeta in tarjetasResponse)
                    {
                        string TarjetaRecibida = tarjeta.numeroTarjetaCompleto;
                        if (tarjeta.estadoTarjeta.Equals("1") || tarjeta.estadoTarjeta.Equals("7"))
                        {
                            if (TarjetaRecibida.StartsWith("678424") || TarjetaRecibida.StartsWith("555906"))
                            {
                                tarjetas.Add(TarjetaRecibida, ClaveParametros.TARJETA_DEBITO);
                            }
                            else
                            {
                                tarjetas.Add(TarjetaRecibida, ClaveParametros.TARJETA_CREDITO);
                            }
                        }

                    }
                }
            }
            catch (TimeoutException timeEx)
            {
                Console.WriteLine(timeEx.Data);
            }

            return tarjetas;
        }

        public async Task<JObject> BloquearCuentaAhorros(string TipoDocumento, string NumeroDocumento, string NumeroCuentaH)
        {
            JObject result = new JObject();
            JObject resultBloq = new JObject();
            JObject resultConfirm = new JObject();

            try
            {

                string Url = _configuration["BUS_REST_ADAPTER"];
                string headerLink = _configuration["BloquearCuentaAhorrosHeader"];
                //consumir servicio bloqueo bus
                JObject Header = ConexionBus.GenerarHeaderBus("BANCA_SERVICES", "CuentasCATSApiGroup", headerLink, "BloquearCuenta");
                JObject Body = ConexionBus.GenerarBody("CuentaDetalles", new JObject() {
                                                                                                        { "numero",NumeroCuentaH },
                                                                                                        { "estado","T" },
                                                                                                        { "motivo","03" }
                                                                                                      }
                );

                JObject requestHeaderOut = ConexionBus.GenerarRequestHeaderOut(Header, Body);

                resultBloq = await Utils.ConsumirApiSalidaJObject(Url, requestHeaderOut);
                resultBloq = JObject.Parse(resultBloq["responseHeaderOut"]["Body"].ToString().Replace("{},", "\"\","));
                result.Add("ResultadoBloqueo", resultBloq);

                Header = ConexionBus.GenerarHeaderBus("BANCA_SERVICES", "CuentasCATSApiGroup", headerLink, "BloquearAprobacion");
                Body = ConexionBus.GenerarBody("CuentaDetalles", new JObject() { { "numero", NumeroCuentaH } });

                requestHeaderOut = ConexionBus.GenerarRequestHeaderOut(Header, Body);

                resultConfirm = await Utils.ConsumirApiSalidaJObject(Url, requestHeaderOut);
                result.Add("ResultadoConfirmacion", JObject.Parse(resultConfirm["responseHeaderOut"]["Body"].ToString().Replace("{},", "\"\",")));

            }
            catch (Exception e)
            {
                result = new JObject {
                                { "codigo", "02" },
                                { "descripcion", "Ha ocurrido un error al bloquear cuenta por primer avance" },
                                { "detalleError", e.ToString() },
                                { "ResultadoBloqueo",resultBloq.ToString() },
                                { "ResultadoConfirmacion", resultConfirm.ToString() }

                            };

                logger.Error($"Error BloquearCuentaAhorros TipoDocumento={TipoDocumento} NumeroDocumento={NumeroDocumento} NumeroCuentaH={NumeroCuentaH} {result.ToString()}");
            }

            return result;
        }
    }
}
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using IBM.Data.DB2.Core;
using System.ServiceModel;
using BancaServices.Domain.Interfaces;
using BancaServices.Application.Services.SerfiUtils;

namespace BancaServices.Application.Services
{
    public class ProductosService : IProductosService
    {
        //private readonly ICROService croService;
        private readonly IASNetServices asNetServices;
        private readonly NLog.ILogger logger = LogManager.GetCurrentClassLogger();
        private readonly IConsultasCms _consultasCms;
        private readonly IConfiguration _configuration;
        private readonly BancaServicesLogsEntities Context;

        public enum Tipo_Productos
        {
            TC = 04,
            PRESTAMO = 02
        }

        public ProductosService(IASNetServices _asNetServices, BancaServicesLogsEntities dbContext, IConsultasCms consultasCms, IConfiguration configuration)
        {
            asNetServices = _asNetServices;
            _consultasCms = consultasCms;
            _configuration = configuration;
            Context = dbContext; // Assign the injected DbContext
        }

        public async Task<JArray> ConsultaCdt(string idCliente)
        {
            var watch = new Stopwatch();
            watch.Start();

            JArray listaCdt = new JArray();
            try
            {
                string endpointUri = _configuration["ConsultaCdtEndPoint"]; // Cambia la URL según tu configuración
                BasicHttpBinding binding = new BasicHttpBinding();
                BancaServices.ConsultaCDTServiceRef.ConsultarCDTClient consultarCDT = new BancaServices.ConsultaCDTServiceRef.ConsultarCDTClient(binding, new EndpointAddress(endpointUri));

                //ConsultaCDTServiceRef.ConsultarCDTClient consultarCDT = new ConsultaCDTServiceRef.ConsultarCDTClient();
                consultarCDT.Open();
                ConsultaCDTServiceRef.consultarCDTResponse consultaCDTRespuesta = await consultarCDT.consultarCDTAsync(idCliente);
                ConsultaCDTServiceRef.responseConsultaSaldoCDTBean cdtBean = consultaCDTRespuesta.@return;
                ConsultaCDTServiceRef.beanConsultaSaldoCDT[] cdts = cdtBean.cuentas;
                if (cdts != null)
                {
                    foreach (ConsultaCDTServiceRef.beanConsultaSaldoCDT cdt in cdts)
                    {

                        JObject producto = new JObject();

                        producto.Add("numProducto", cdt.numeroCuenta);
                        producto.Add("codProducto", "01-CDT");
                        producto.Add("nomProducto", cdt.referencia);
                        string strSaldo = cdt.saldo.Replace("$", string.Empty);
                        strSaldo = strSaldo.Replace(".", string.Empty).Trim();
                        strSaldo = convertToDecimal(strSaldo).Replace(",", ".");
                        //long saldo=0;
                        //if (!String.IsNullOrEmpty(strSaldo))
                        //{
                        //    saldo = long.Parse(strSaldo);
                        //}
                        producto.Add("saldo", strSaldo);
                        var fechaEmision = DateTime.Parse(cdt.fechaEmision);
                        producto.Add("fechaEmision", fechaEmision.ToString("yyyy/MM/dd"));
                        producto.Add("estado", cdt.estado);
                        producto.Add("tipoProducto", "01");
                        producto.Add("categoria", "01");
                        listaCdt.Add(producto);
                    }
                }
                watch.Stop();
                logger.Info($"Tiempo Respuesta ConsultaCdt: {watch.ElapsedMilliseconds} milisegundos");
                return listaCdt;

            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return listaCdt;
            }

        }

        public async Task<JArray> ConsultaCMS(string tipoId, string idCliente, bool muestraBloqueo)
        {
            var watch = new Stopwatch();
            watch.Start();
            JArray listaProductoCms = new JArray();
            try
            {

                string urlResumenCms = string.Format("{0}{1}{2}/{3}", _configuration["appsettings:Url_Services_CMS"], _configuration["Producto_Resumen_CMS"], tipoId, idCliente);
                //21-03-2023 no se puede cambiar a restsharp por que molesta
                JObject resultado = await ConsumirApiRest.ConsumirApiSalidaJObject(urlResumenCms, null, ConsumirApiRest.METODO_GET, logger: logger);
                if (resultado != null && resultado.ContainsKey("results"))
                {
                                       var productosCms = resultado.GetValue("results").Value<JArray>();
                    productosCms = JArray.FromObject(productosCms.Where(x => !string.IsNullOrEmpty(x["numeroCuenta"].ToString())).ToArray());
                    foreach (JObject productoCms in productosCms)
                    {
                        try
                        {
                            JObject producto = new JObject();
                            var estado = productoCms.GetValue("estado").Value<JObject>();
                            var vector = estado.GetValue("vector").Value<string>();
                            var descripcionEstado = estado.GetValue("descripcion").Value<string>();
                            var strSaldo = productoCms.GetValue("saldo").Value<string>();
                            var codigo = productoCms.GetValue("codigo").Value<string>();
                            var codigoMercado = productoCms.GetValue("codMercado").Value<string>();
                            double saldo = double.Parse(strSaldo);

                            if (!muestraBloqueo && !estadoEsValido(vector) && saldo == 0)
                            {
                                continue;
                            }
                            else if (string.IsNullOrEmpty(codigo))
                            {
                                continue;
                            }
                            else if (validateMarketCode(codigoMercado, vector, descripcionEstado))
                            {
                                continue;

                            }

                            var numero = productoCms.GetValue("numeroCuenta").Value<string>();
                            var nombre = productoCms.GetValue("nombre").Value<string>();
                            var tipo = productoCms.GetValue("tipo").Value<string>();
                            tipo = tipo.Equals("88") ? "04" : tipo;

                            var fechaEmision = productoCms.GetValue("fechaEmision").Value<string>();
                            DateTime fechaEmi = new DateTime();
                            try
                            {
                                fechaEmi = DateTime.ParseExact(fechaEmision, "yyyyMMdd", CultureInfo.InvariantCulture);
                            }
                            catch
                            {
                                fechaEmi = DateTime.ParseExact("19000101", "yyyyMMdd", CultureInfo.InvariantCulture);
                            }

                            var strDisponible = productoCms.GetValue("disponible").Value<string>().Replace('.', ',');

                            producto.Add("numProducto", numero);
                            producto.Add("codProducto", codigo);
                            producto.Add("nomProducto", nombre);
                            producto.Add("tipoProducto", tipo);
                            producto.Add("categoria", "04");
                            producto.Add("estado", descripcionEstado);
                            producto.Add("fechaEmision", fechaEmi.ToString("yyyy/MM/dd"));
                            double disponible = 0;
                            if (!strDisponible.Equals(",00"))
                            {
                                disponible = double.Parse(strDisponible);
                            }
                            double dispoAsNet = double.NaN;
                            if (numero.StartsWith("8999"))
                            {
                                string numProducto = await _consultasCms.ObtenerTarjetaPorNumRotativo(numero);
                                if (!string.IsNullOrEmpty(numProducto))
                                {
                                    dispoAsNet = await asNetServices.ObtenerCupoProducto(numProducto, ASNetServices.TipoProductos.Rotativo);
                                }
                            }
                            else
                            {

                                dispoAsNet = await asNetServices.ObtenerCupoProducto(numero, ASNetServices.TipoProductos.TarjetaCredito);
                            }
                            if (!double.IsNaN(dispoAsNet))
                            {
                                disponible = dispoAsNet;
                            }
                            producto.Add("disponible", string.Format("{0:0}", disponible));
                            producto.Add("saldo", (long)saldo);

                            if (tipoId.Equals("03") || tipoId.Equals("3") && codigo.Equals("88"))
                            {
                                //NIT Validar TC Empresarial
                                producto["tipoTCE"] = await _consultasCms.EsTarjetaEmpresarialPadre(numero) ? "Padre" : "Hija";
                            }

                            listaProductoCms.Add(producto);
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex);
                            LogEventInfo info = new LogEventInfo(NLog.LogLevel.Error, ex.Source, ex.Message);
                            info.Properties["stacktrace"] = ex.StackTrace;
                            info.Properties["innerexceptionmessage"] = ex.InnerException;
                            info.Properties["tipo_id"] = tipoId;
                            info.Properties["id_cliente"] = idCliente;
                            logger.Log(info);
                        }


                    }
                }

            }
            catch (Exception ex)
            {
                logger.Error(ex);
                LogEventInfo info = new LogEventInfo(NLog.LogLevel.Error, ex.Source, ex.Message);
                info.Properties["stacktrace"] = ex.StackTrace;
                info.Properties["innerexceptionmessage"] = ex.InnerException;
                info.Properties["tipo_id"] = tipoId;
                info.Properties["id_cliente"] = idCliente;
                logger.Log(info);

            }
            watch.Stop();

            logger.Info($"Tiempo Respuesta ConsultaCMS: {watch.ElapsedMilliseconds} milisegundos");
            return listaProductoCms;
        }

        private bool validateMarketCode(string codigoMercado, string vector, string descripcion)
        {
            char[] vectorEstado = vector.ToCharArray();

            bool result = false;
            try
            {
                if (vectorEstado[8].Equals('1'))
                {


                    var parametros = Context.Parametros.Where(x => x.DescripcionParametro == "MarketCode").FirstOrDefault();
                    if (parametros != null)
                    {
                        var marketsCode = parametros.ValorParametro.Split(',');
                        foreach (var item in marketsCode)
                        {
                            if (item == codigoMercado)
                            {
                                result = true;
                                break;
                            }
                        }
                    }

                }
            }
            catch (Exception ex)
            {

                logger.Error(ex);
            }
            return result;
        }

        public async Task<JArray> ConsultarPrestamos(string tipoId, string idCliente)
        {
            var watch = new Stopwatch();
            watch.Start();

            JArray listaPrestamos = new JArray();
            var tipoIdEibs = Utils.HomologarTipoId(tipoId, Utils.Sistema.EIBS);
            try
            {
                ConsultaRecaudoFacturaSR.ConsultasRecaudoFacturacionClient recaudoFacturacion = new ConsultaRecaudoFacturaSR.ConsultasRecaudoFacturacionClient();
                recaudoFacturacion.Open();
                var reqPrestamos = await recaudoFacturacion.ListarPrestamosAsync(tipoIdEibs, idCliente);
                var lPrestamos = reqPrestamos.@return;
                if (lPrestamos.estado.Equals("true"))
                {
                    if (lPrestamos.cuentas != null)
                    {
                        JObject producto;
                        foreach (ConsultaRecaudoFacturaSR.cuentasPorCobrarWSBean cuenta in lPrestamos.cuentas)
                        {
                            if (!string.IsNullOrEmpty(cuenta.cuenta.numProducto) && !string.IsNullOrEmpty(cuenta.cuenta.codProducto))
                            {

                                producto = new JObject();
                                producto.Add("numProducto", cuenta.cuenta.numProducto);
                                producto.Add("codProducto", cuenta.cuenta.codProducto);
                                producto.Add("nomProducto", cuenta.cuenta.descripcion.Trim());
                                producto.Add("tipoProducto", "02");
                                producto.Add("categoria", "02");
                                producto.Add("disponible", 0);
                                producto.Add("estado", cuenta.descripcionEstado.ToUpper());
                                producto.Add("fechaEmision", cuenta.cuenta.fechaDesembolso.ToString("yyyy/MM/dd"));
                                producto.Add("saldo", convertToDecimal(cuenta.cuenta.pagoTotal.ToString()).Replace(',', '.'));

                                listaPrestamos.Add(producto);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error ConsultarPrestamos {ex.ToString()}");
            }

            watch.Stop();

            logger.Info($"Tiempo Respuesta ConsultarPrestamos: {watch.ElapsedMilliseconds} milisegundos tipoId:{tipoId} IdCliente:{idCliente}");
            return listaPrestamos;
        }

        public async Task<JArray> ConultaCuantas(string idCliente)
        {
            var watch = new Stopwatch();
            watch.Start();

            JArray listaCuentas = new JArray();
            try
            {
                ConsultaCuentasServiceRef.ConsultarSaldoCuentasClient consultaSaldoCLient = new ConsultaCuentasServiceRef.ConsultarSaldoCuentasClient();
                consultaSaldoCLient.Open();
                ConsultaCuentasServiceRef.responseConsultaSaldoCTAHBean responseConsultaSaldo = await consultaSaldoCLient.consultarSaldosCTAHAsync(idCliente);
                ConsultaCuentasServiceRef.beanConsultaSaldoCTAH[] cuentas = responseConsultaSaldo.cuentas;
                if (cuentas != null)
                {
                    foreach (ConsultaCuentasServiceRef.beanConsultaSaldoCTAH cuenta in cuentas)
                    {

                        JObject producto = new JObject();
                        producto.Add("numProducto", cuenta.numeroCuenta);
                        producto.Add("codProducto", cuenta.codProducto);
                        producto.Add("nomProducto", cuenta.referencia.Trim());
                        producto.Add("tipoProducto", "05");
                        producto.Add("categoria", "05");
                        producto.Add("estado", cuenta.estado.Trim());
                        producto.Add("fechaEmision", cuenta.fechaEmision);
                        string strSaldo = cuenta.saldo.Replace("$", string.Empty).Replace(".", string.Empty).Replace(" ", string.Empty).Trim();
                        strSaldo = strSaldo.Replace(',', '.');
                        double saldo = 0;
                        if (!string.IsNullOrEmpty(strSaldo))
                        {
                            saldo = double.Parse(strSaldo);
                        }
                        producto.Add("saldo", strSaldo);
                        listaCuentas.Add(producto);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return null;
            }
            watch.Stop();
            Debug.WriteLine($"Tiempo Respuesta ConultaCuantas: {watch.ElapsedMilliseconds} milisegundos");
            return listaCuentas;
        }

        private bool estadoEsValido(string estado)
        {
            char[] vectorEstado = estado.ToCharArray();

            if (!vectorEstado[0].Equals('0') || !vectorEstado[9].Equals('0'))
            {
                return false;

            }
            //else if (vectorEstado[8].Equals('1'))
            //{
            //    return false;
            //}

            return true;
        }

        public async Task<JObject> DetalleCMS(string tipoId, string idCliente, string numProd)
        {
            JObject productoCms = new JObject();
            try
            {
                string urlResumenCms = string.Format("{0}{1}", _configuration["appsettings:Url_Services_CMS"], _configuration["appsettings:Producto_Detalle_CMS"]);

                var requestBody = new JObject();
                requestBody.Add("typeId", tipoId);
                requestBody.Add("clientId", idCliente);
                requestBody.Add("numProd", numProd);

                JObject resultado = await ConsumirApiRest.ConsumirApiSalidaJObject(urlResumenCms, requestBody, ConsumirApiRest.METODO_POST, logger: logger);

                if (resultado != null && resultado.ContainsKey("result"))
                {
                    productoCms = resultado.GetValue("result").Value<JObject>();
                }



            }
            catch (Exception ex)
            {
                logger.Error(ex);
                JObject result = new JObject();
                result.Add("Error", ex.ToString());
                return null;
            }
            return productoCms;
        }

        public async Task<JArray> ConsultaCuentaCorriente(string tipoId, string idCliente, string operacion)
        {
            var watch = new Stopwatch();
            watch.Start();
            JArray cuentasCorriente = new JArray();
            try
            {

                var tipoIdHomo = Utils.HomologarTipoId(tipoId, Utils.Sistema.EIBS);
                string urlResumenCms = string.Format("{0}{1}/{2}", _configuration["appsettings:Url_Cuenta_Corriente"], idCliente, tipoIdHomo);
                JObject resultado = await ConsumirApiRestSharp.RestSharpApiSalidaJObject(urlResumenCms, null, RestSharp.Method.Get, logger: logger);
                if (resultado != null && resultado.ContainsKey("estado"))
                {
                    var estado = resultado.GetValue("estado").Value<bool>();
                    if (estado)
                    {
                        var data = resultado.GetValue("data").Value<JObject>();
                        var codRespuesta = data.GetValue("codigo").Value<string>();
                        if (codRespuesta.Equals("00"))
                        {
                            var listaCuentas = data.GetValue("object").Value<JArray>();
                            foreach (JObject cuenta in listaCuentas)
                            {
                                JObject cuentaCorriente = new JObject();
                                var numCuenta = cuenta.GetValue("numeroCuenta").Value<string>();
                                cuentaCorriente.Add("numProducto", numCuenta);
                                cuentaCorriente.Add("codProducto", "01-CC");
                                cuentaCorriente.Add("nomProducto", cuenta.GetValue("nomProducto").Value<string>().Trim());
                                cuentaCorriente.Add("tipoProducto", "06");
                                cuentaCorriente.Add("categoria", "06");
                                cuentaCorriente.Add("estado", cuenta.GetValue("estado").Value<string>());
                                cuentaCorriente.Add("fechaEmision", cuenta.GetValue("fechaEmision").Value<string>());
                                string strSaldo = cuenta.GetValue("saldo").Value<string>();
                                cuentaCorriente.Add("saldo", Utils.LimpiarSaldos(strSaldo));

                                if (operacion.Equals(Utils.DETALLE))
                                {
                                    string cupoSobregiro = cuenta.GetValue("cupoSobregiro").Value<string>();
                                    cuentaCorriente.Add("cupoSobregiro", Utils.LimpiarSaldos(cupoSobregiro));
                                    string disponibleSobregiro = cuenta.GetValue("disponibleSobregiro").Value<string>();
                                    cuentaCorriente.Add("disponibleSobregiro", Utils.LimpiarSaldos(disponibleSobregiro));
                                    cuentaCorriente.Add("referencia", cuenta.GetValue("referencia").Value<string>());

                                    string saldoRetenido = cuenta.GetValue("saldoRetenido").Value<string>();
                                    cuentaCorriente.Add("saldoRetenido", Utils.LimpiarSaldos(saldoRetenido));

                                    string saldoCanje = cuenta.GetValue("saldoCanje").Value<string>();
                                    cuentaCorriente.Add("saldoCanje", Utils.LimpiarSaldos(saldoCanje));
                                }

                                cuentasCorriente.Add(cuentaCorriente);
                            }
                        }
                    }
                }


            }
            catch (Exception ex)
            {
                logger.Error(ex);

            }
            watch.Stop();

            logger.Info($"Tiempo Respuesta ConsultaCuentaCorriente: {watch.ElapsedMilliseconds} milisegundos");
            return cuentasCorriente;

        }

        public async Task<JObject> DetallePrestamo(string tipoId, string idCliente, string numProducto)
        {
            LogEventInfo theEvent = new LogEventInfo(NLog.LogLevel.Info, "DetallePrestamo", "Consulta de detalle de prestamo");
            //theEvent.Properties["tipoId"] = tipoId;
            //theEvent.Properties["idCliente"] = idCliente;
            //theEvent.Properties["numProducto"] = numProducto;
            logger.Info(theEvent);

            JArray prestamos = new JArray();
            var tipoIdEibs = Utils.HomologarTipoId(tipoId, Utils.Sistema.EIBS);
            JObject producto = null;
            JObject respuesta = new JObject();
            try
            {
                ConsultaRecaudoFacturaSR.ConsultasRecaudoFacturacionClient recaudoFacturacion = new ConsultaRecaudoFacturaSR.ConsultasRecaudoFacturacionClient();
                var reqPrestamos = await recaudoFacturacion.ListarPrestamosAsync(tipoIdEibs, idCliente);
                var lPrestamos = reqPrestamos.@return;
                if (lPrestamos.estado.Equals("true"))
                {

                    foreach (ConsultaRecaudoFacturaSR.cuentasPorCobrarWSBean cuenta in lPrestamos.cuentas)
                    {
                        if (cuenta.cuenta.numProducto.Equals(numProducto))
                        {

                            //string test = cuenta.cuenta.valorDesembolso.ToString("0.##");
                            producto = new JObject();
                            producto.Add("referencia", cuenta.cuenta.numProducto);
                            producto.Add("fechaEmision", cuenta.cuenta.fechaDesembolso.ToString("yyyyMMdd"));
                            producto.Add("fechalimitePago", cuenta.cuenta.vencimientoCuota.ToString("yyyyMMdd"));
                            producto.Add("pagoMinimo", convertToDecimal(cuenta.cuenta.valorCuotaCorriente.ToString()).Replace(',', '.'));
                            producto.Add("pagominimoVencido", convertToDecimal(cuenta.cuenta.valorCuotasVencidas.ToString()).Replace(',', '.'));
                            producto.Add("pagoTotal", convertToDecimal(cuenta.cuenta.pagoTotal.ToString()).Replace(',', '.'));
                            producto.Add("saldoCapital", convertToDecimal(cuenta.cuenta.saldoCapital.ToString()).Replace(',', '.'));
                            producto.Add("valorDesembolso", convertToDecimal(cuenta.cuenta.valorDesembolso.ToString("0.##")).Replace(',', '.'));
                            producto.Add("estado", cuenta.descripcionEstado);
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                LogEventInfo info = new LogEventInfo(NLog.LogLevel.Error, ex.Source, ex.Message);
                info.Properties["stacktrace"] = ex.StackTrace;
                info.Properties["innerexceptionmessage"] = ex.InnerException;
                info.Properties["tipo_id"] = tipoId;
                info.Properties["id_cliente"] = idCliente;
                info.Properties["num_producto"] = numProducto;
                logger.Log(info);
                respuesta.Add("codigo", "002");
                respuesta.Add("descripcion", "Ocurrió un error " + ex.Message);
                return respuesta;
            }
            if (producto == null)
            {
                respuesta.Add("codigo", "001");
                respuesta.Add("descripcion", "No se encontró el producto");
                return respuesta;

            }
            respuesta.Add("codigo", "000");
            respuesta.Add("descripcion", "Transacción exitosa");
            respuesta.Add("prestamo", producto);
            return respuesta;
        }

        private string convertToDecimal(string valor)
        {
            string varFinal;
            try
            {
                string[] spliteado = valor.Split(',');
                if (spliteado.Length == 2)
                {
                    switch (spliteado[1].Length)
                    {
                        case 0:
                            varFinal = spliteado[0] + ",00";
                            break;
                        case 1:
                            varFinal = spliteado[0] + "," + spliteado[1] + "0";
                            break;
                        case 2:
                            varFinal = spliteado[0] + "," + spliteado[1];
                            break;
                        default:
                            varFinal = valor;
                            break;

                    }
                }
                else
                {
                    varFinal = string.Format("{0},00", valor);
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
            return varFinal;
        }

        public async Task<bool> TienePeriodoGracia(string tarjeta)
        {
            try
            {

                string urlPeriodoGracia = _configuration["appsettings:URL_Servicio_PeridoGracia"].ToString();

                var requestBody = new JObject();
                requestBody.Add("Tarjeta", tarjeta);

                JObject resultado = await ConsumirApiRest.ConsumirApiSalidaJObject(urlPeriodoGracia, requestBody, ConsumirApiRest.METODO_POST, logger: logger);
                var codigo = resultado.GetValue("Codigo").Value<string>();
                return codigo.Equals("00");


            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return false;
            }
        }

        public double PagoMinPeriodoGracia(string referencia)
        {
            double pagoMinimo = 0;
            string connString = _configuration.GetConnectionString("FACT");

            DB2DataReader reader;
            try
            {
                using (DB2Connection conn = new DB2Connection(connString))
                {
                    string query = @"SELECT ALVCUOTA FROM PHSALIC WHERE ALREFC = @referencia";

                    conn.Open();

                    using (DB2Command command = new DB2Command(query, conn))
                    {
                        command.CommandType = CommandType.Text;
                        command.Parameters.Add(new DB2Parameter("@referencia", DB2Type.VarChar));
                        command.Parameters["@referencia"].Value = referencia;

                        reader = command.ExecuteReader();
                        if (reader.Read())
                        {
                            pagoMinimo = reader.GetDouble(0);
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
            return pagoMinimo;
        }

        //CTAH

        public async Task<JArray> ConsultarSaldosCTAH(string idCliente)
        {
            JArray listaCuentas = new JArray();
            var watch = new Stopwatch();
            watch.Start();
            try
            {
                string urlResumenCTAH = string.Format("{0}{1}{2}", _configuration["appsettings:Url_Services_Ahorro"], "consultarSaldosCTAH/", idCliente);
                int TimeoutServicesAhorro = int.Parse(_configuration["appsettings:TimeoutServicesAhorro"]);
                JObject resultado = await ConsumirApiRestSharp.RestSharpApiSalidaJObject(urlResumenCTAH, null, RestSharp.Method.Get, logger: logger, Timeout: TimeoutServicesAhorro);
                if (resultado != null && resultado.ContainsKey("cuentas"))
                {
                    var cuentas = resultado.GetValue("cuentas").Value<JArray>();

                    foreach (JObject cuenta in cuentas.OfType<JObject>())
                    {
                        JObject producto = new JObject();
                        producto.Add("numProducto", cuenta.GetValue("numeroCuenta").ToString());
                        producto.Add("codProducto", cuenta.GetValue("codProducto").ToString());
                        producto.Add("nomProducto", cuenta.GetValue("referencia").ToString().Trim());
                        producto.Add("tipoProducto", "05");
                        producto.Add("categoria", "05");
                        producto.Add("estado", cuenta.GetValue("estado").ToString().Trim());
                        producto.Add("fechaEmision", cuenta.ContainsKey("fechaEmision") ? cuenta.GetValue("fechaEmision").ToString() : "");
                        producto.Add("saldo", Utils.FormatoSaldo(cuenta.GetValue("saldo").ToString()));
                        producto.Add("producto", cuenta.GetValue("producto").ToString());
                        producto.Add("subProducto", cuenta.GetValue("subProducto").ToString());
                        producto.Add("numTarjeta", cuenta.GetValue("numTarjeta").ToString());
                        listaCuentas.Add(producto);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error<string>($"No se puede ConsultarSaldosCTAH IdCliente: {idCliente} {JsonConvert.SerializeObject(ex)}");
            }

            watch.Stop();

            logger.Info($"Tiempo Respuesta ConsultarSaldosCTAH: {watch.ElapsedMilliseconds} milisegundos");
            return listaCuentas;
        }

        public async Task<JArray> ConsultarSaldosCTA(string idCliente, string tipoId, string codProducto)
        {
            JArray listaCuentas = new JArray();
            try
            {
                string urlResumenCTAH = string.Format("{0}{1}{2}/{3}/{4}", _configuration["appsettings:Url_Services_Ahorro"], "consultarSaldoCta/", idCliente, tipoId, codProducto);
                JObject resultado = await ConsumirApiRest.ConsumirApiSalidaJObject(urlResumenCTAH, null, ConsumirApiRest.METODO_GET, logger: logger);
                if (resultado != null && resultado.ContainsKey("cuentas"))
                {
                    var cuentas = resultado.GetValue("cuentas").Value<JArray>();

                    foreach (JObject cuenta in cuentas.OfType<JObject>())
                    {
                        JObject producto = new JObject();
                        producto.Add("estado", cuenta.GetValue("estado").ToString());
                        producto.Add("idCliente", cuenta.GetValue("idCliente").ToString());
                        producto.Add("nroCuenta", cuenta.GetValue("nroCuenta").ToString());
                        producto.Add("saldoCanje", Utils.FormatoSaldo(cuenta.GetValue("saldoCanje").ToString()));
                        producto.Add("saldoDisp", Utils.FormatoSaldo(cuenta.GetValue("saldoDisp").ToString()));
                        producto.Add("saldoMin", Utils.FormatoSaldo(cuenta.GetValue("saldoMin").ToString()));
                        producto.Add("saldoNeto", cuenta.GetValue("saldoNeto").ToString());
                        producto.Add("saldoRetenido", cuenta.GetValue("saldoRetenido").ToString());
                        producto.Add("saldoTotal", Utils.FormatoSaldo(cuenta.GetValue("saldoTotal").ToString()));
                        listaCuentas.Add(producto);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return null;
            }
            return listaCuentas;
        }

        public async Task<JArray> ConsultarSaldosCTAByNroCuenta(string idCliente, string tipoId, string nroCuenta, string codProducto)
        {
            JArray listaCuentas = new JArray();
            try
            {
                string urlResumenCTAH = string.Format("{0}{1}{2}/{3}/{4}/{5}", _configuration["appsettings:Url_Services_Ahorro"], "consultarSaldoCtaByNroCuenta/", idCliente, tipoId, nroCuenta, codProducto);
                JObject resultado = await ConsumirApiRest.ConsumirApiSalidaJObject(urlResumenCTAH, null, ConsumirApiRest.METODO_GET, logger: logger);
                if (resultado != null && resultado.ContainsKey("cuenta"))
                {
                    var cuenta = resultado.GetValue("cuenta").Value<JObject>();

                    JObject producto = new JObject();
                    producto.Add("estado", cuenta.GetValue("estado").ToString());
                    producto.Add("idCliente", cuenta.GetValue("idCliente").ToString());
                    producto.Add("nroCuenta", cuenta.GetValue("nroCuenta").ToString());
                    producto.Add("saldoCanje", Utils.FormatoSaldo(cuenta.GetValue("saldoCanje").ToString()));
                    producto.Add("saldoDisp", Utils.FormatoSaldo(cuenta.GetValue("saldoDisp").ToString()));
                    producto.Add("saldoMin", Utils.FormatoSaldo(cuenta.GetValue("saldoMin").ToString()));
                    producto.Add("saldoNeto", cuenta.GetValue("saldoNeto").ToString());
                    producto.Add("saldoRetenido", cuenta.GetValue("saldoRetenido").ToString());
                    producto.Add("saldoTotal", Utils.FormatoSaldo(cuenta.GetValue("saldoTotal").ToString()));
                    listaCuentas.Add(producto);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return null;
            }
            return listaCuentas;
        }

        public async Task<JObject> ConsultaTipoCuenta(string NumCuenta)
        {
            JObject result = new JObject();
            JObject Body = new JObject();
            string Url = _configuration["BUS_REST_ADAPTER"];

            string NAME_BUS = "ProductosDetalleApiGroup";
            string NAMESPACE_BUS = _configuration["BloquearCuentaAhorrosHeader"];
            string OPERACION_BUS = "TipoCuenta";

            JObject Header = ConexionBus.GenerarHeaderBus("BANCA_SERVICES", NAME_BUS, NAMESPACE_BUS, OPERACION_BUS);
            Body.Add(OPERACION_BUS, new JObject() {
                {"numCuenta", NumCuenta }
            });

            JObject requestHeaderOut = new JObject();
            requestHeaderOut.Add("requestHeaderOut", new JObject() {
                {"Header", Header },
                { "Body", Body }
            });

            try
            {
                result = await Utils.ConsumirApiSalidaJObject(Url, requestHeaderOut);
                result = JObject.Parse(result["responseHeaderOut"]["Body"]["respuesta"]["data"].ToString().Replace("{},", "\"\","));

            }
            catch (Exception e)
            {
                result = new JObject
                {
                    { "codigo", "02" },
                    { "descripcion", "Ha ocurrido un error al Consumir el servicio, intente mas tarde" },
                    { "detalleError", e.Message }
                };
            }
            return result;
        }

        public async Task<JArray> ConsultarConvenioEfectivo(string tipoId, string idCliente)
        {
            JArray listaCuentas = new JArray();
            var watch = new Stopwatch();
            watch.Start();
            try
            {
                string HomoTipoId = Utils.HomologarTipoId(tipoId, Utils.Sistema.EIBS);
                string urlConvenioEfectivoInscripciones = string.Format(_configuration["appsettings:url_Convenio_ConsultarInscripcion"], HomoTipoId, idCliente);

                JObject resultado = await ConsumirApiRestSharp.RestSharpApiSalidaJObject(urlConvenioEfectivoInscripciones, null, RestSharp.Method.Get, logger: logger);
                if (resultado != null && (bool)resultado["estado"] && resultado.ContainsKey("data"))
                {
                    var cuentas = resultado.GetValue("data").Value<JArray>();
                    foreach (JObject cuenta in cuentas.OfType<JObject>())
                    {
                        string urlConvenioEfectivoConsulta = string.Format(_configuration["appsettings:url_Convenio_ConsultarSaldo"], HomoTipoId, idCliente, cuenta.GetValue("numCuenta").ToString());
                        JObject resultadoSaldo = await ConsumirApiRestSharp.RestSharpApiSalidaJObject(urlConvenioEfectivoConsulta, null, RestSharp.Method.Get, logger: logger);
                        string consultaSaldo = "0";
                        string fechaEmision = "";
                        if (resultadoSaldo != null && (bool)resultadoSaldo["estado"] && resultadoSaldo.ContainsKey("data"))
                        {
                            var consultas = resultadoSaldo.GetValue("data").Value<JArray>();
                            foreach (JObject consulta in consultas.OfType<JObject>())
                            {
                                consultaSaldo = consulta.GetValue("saldo").ToString();
                                fechaEmision = consulta.GetValue("fechaInscripcion").ToString();
                            }
                        }

                        JObject producto = new JObject();
                        producto.Add("numProducto", cuenta.GetValue("numCuenta").ToString());
                        producto.Add("codProducto", cuenta.GetValue("codConvenio").ToString());
                        producto.Add("nomProducto", "Convenio Efectivo");
                        producto.Add("tipoProducto", "07");
                        producto.Add("categoria", "07");
                        producto.Add("estado", cuenta.GetValue("activo").ToString() == "1" ? "Activo" : "Inactivo");
                        producto.Add("saldo", consultaSaldo);
                        producto.Add("fechaEmision", fechaEmision);
                        listaCuentas.Add(producto);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return null;
            }

            watch.Stop();
            logger.Info($"Tiempo Respuesta ConsultaConvenioEfectivo: {watch.ElapsedMilliseconds} milisegundos");
            return listaCuentas;
        }
    }
}

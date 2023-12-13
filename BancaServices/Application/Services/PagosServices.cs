using Newtonsoft.Json.Linq;
using NLog;
using System.Data.Entity;
using System.Globalization;
using System.Net.Http.Headers;
using IBM.Data.DB2.Core;
using BancaServices.Models;
using BancaServices.PagoTcoAsNet;
using BancaServices.Domain.Interfaces;
using BancaServices;
using BancaServices.Application.Services.SerfiUtils;

namespace BancaServices.Application.Services
{
    public class PagosServices : IPagosServices
    {
        private readonly ICashServices cashServices;
        private readonly NLog.ILogger logger = LogManager.GetCurrentClassLogger();
        private readonly IProductosService _producServices;
        private readonly IConfiguration _configuration;
        private readonly BancaServicesLogsEntities Context; // Inject the DbContext

        public PagosServices(ICashServices cashServices, BancaServicesLogsEntities dbContext, IProductosService productos, IConfiguration configuration)
        {
            this.cashServices = cashServices;
            _producServices = productos;
            _configuration = configuration;
            Context = dbContext; // Assign the injected DbContext
        }

        public async Task<JObject> RealizarPago(JObject parameters)
        {
            #region Preparacion de variables

            JObject respusta = new JObject();
            string idCliente = parameters.GetValue("idCliente").Value<string>();
            string numProducto = parameters.GetValue("numProducto").Value<string>();
            decimal monto = decimal.Zero;
            string cuentaOrigen = parameters.GetValue("cuentaOrigen").Value<string>();

            string idDeudor = string.Empty;
            string tipoIdDeudor = string.Empty;

            var esPagoTercero = parameters["EsPagoTercero"] != null ? parameters["EsPagoTercero"].Value<bool>() : false;

            bool puedePagar;
            if (!esPagoTercero)
            {
                monto = parameters.GetValue("monto").Value<decimal>();
                puedePagar = PuedePagar(numProducto, idCliente, monto);
                tipoIdDeudor = parameters.GetValue("tipoId").Value<string>();
            }
            else
            {
                CultureInfo culture = new CultureInfo("es-CO");
                monto = Convert.ToDecimal(parameters.Value<string>("monto"), culture);
                idDeudor = parameters.GetValue("idDeudor").Value<string>();
                tipoIdDeudor = parameters.GetValue("tipoIdDeudor").Value<string>();
                puedePagar = PuedePagar(numProducto, idDeudor, monto);
            }

            if (!puedePagar)
            {
                respusta.Add("codigo", "30");
                respusta.Add("descripcion", "El monto excede el pago total");
                return respusta;
            }

            PagosEibsCms log = new PagosEibsCms();
            log.TipoId = parameters.GetValue("tipoId").Value<string>();
            log.IdCliente = idCliente;
            log.NumProducto = numProducto;
            log.CuentaOrigen = cuentaOrigen;
            log.Monto = monto;
            log.Fecha = DateTime.Now.Date;
            log.Hora = DateTime.Now.TimeOfDay;
            #endregion
            try
            {

                #region New CashOut
                string canalReq = parameters.ContainsKey("canal") ? parameters.GetValue("canal").Value<string>() : "1";
                ResponseTRNCodeModel responseTRNCode = new ResponseTRNCodeModel();
                RequestTRNCodeModel requestTRNCode = new RequestTRNCodeModel();
                requestTRNCode.creationUser = _configuration["UsrTRNCode"];
                requestTRNCode.environment = _configuration["EnvironmentTRNCode"];
                requestTRNCode.transactionCode = _configuration["TransactionCodeTRNCode"];
                responseTRNCode = await cashServices.TransactionCodeTRN(requestTRNCode);

                if (responseTRNCode.code.Equals("000"))
                {
                    string canalCashOut = "";
                    if (canalReq == "1")
                    {
                        canalCashOut = _configuration["CanalWebCashOut"];
                    }
                    else
                    {
                        canalCashOut = _configuration["CanalApiCashOut"];
                    }
                    RequestCashOutModel requestCashOutModel = new RequestCashOutModel();
                    requestCashOutModel.environment = _configuration["EnvironmentTRNCode"];
                    requestCashOutModel.trn = _configuration["TrnCashOut"];
                    requestCashOutModel.codTrn = _configuration["TransactionCodeTRNCode"];
                    requestCashOutModel.codCnl = canalCashOut;
                    requestCashOutModel.codTmn = _configuration["UsrCashOut"];
                    requestCashOutModel.ctaDes = cuentaOrigen;
                    requestCashOutModel.ctaHas = _configuration["AccountCountableCashOut"];
                    requestCashOutModel.transactionCode = responseTRNCode.data.code;
                    requestCashOutModel.amount = string.Format("{0:0.00}", monto).Replace(",", string.Empty);
                    requestCashOutModel.date = DateTime.Now.ToString("yyyyMMdd");

                    ResponseCashOutModel responseCashOutModel = await cashServices.CashOut(requestCashOutModel);

                    if (responseCashOutModel.successful && responseCashOutModel.code.Equals("0000"))
                    {
                        long ultimoCons = long.MinValue;
                        string consecutivo = string.Empty;
                        try
                        {
                            if (esPagoTercero)
                            {
                                consecutivo = getConsecutivoPago(idDeudor, out ultimoCons);
                            }
                            else
                            {
                                consecutivo = getConsecutivoPago(idCliente, out ultimoCons);
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.Error($"Error en getConsecutivoPago {ex.ToString()}");
                            log.CodigoRespuesta = "9014";
                            log.DescripcionRespuesta = "Error al consultar el consecutivo de pago";
                            respusta.Add("codigo", "03");
                            respusta.Add("descripcion", string.Format("{0} - [{1}]", "Error al consultar el consecutivo", ex.Message));
                        }
                        if (!string.IsNullOrEmpty(consecutivo))
                        {
                            int inserto = 0;
                            try
                            {
                                if (esPagoTercero)
                                {
                                    inserto = insertaPago(idDeudor, monto, responseTRNCode.data.code, consecutivo, numProducto);
                                }
                                else
                                {
                                    inserto = insertaPago(idCliente, monto, responseTRNCode.data.code, consecutivo, numProducto);
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Error(ex);
                                log.CodigoRespuesta = "9004";
                                log.DescripcionRespuesta = "Error al registrar el pago en tabla PHSPSE";
                                respusta.Add("codigo", "02");
                                respusta.Add("descripcion", string.Format("{0} - [{1}]", "Error al registrar el pago", ex.Message));
                            }
                            if (inserto > 0)
                            {
                                if (esPagoTercero)
                                {
                                    actualizaConsecutivo(idDeudor, ultimoCons);
                                }
                                else
                                {
                                    actualizaConsecutivo(idCliente, ultimoCons);
                                }
                                log.CodigoRespuesta = responseCashOutModel.code;
                                log.DescripcionRespuesta = responseCashOutModel.message;
                                respusta.Add("codigo", "00");
                                respusta.Add("descripcion", "Transacción exitosa");
                                ResponsePagoTarjetaCreditoDTO responseAsnet = null;

                                if (esPagoTercero)
                                {
                                    //responseAsnet = await AplicarPagoAutorizador(idDeudor, numProducto, montoAuth);
                                    responseAsnet = await PagoAutorizador(numProducto, idDeudor, tipoIdDeudor, monto.ToString());
                                }
                                else
                                {
                                    //responseAsnet = await AplicarPagoAutorizador(idCliente, numProducto, montoAuth);
                                    responseAsnet = await PagoAutorizador(numProducto, idCliente, tipoIdDeudor, monto.ToString());
                                }
                                if (responseAsnet != null)
                                {
                                    log.respuestaAuto = responseAsnet.codigoRespuesta;
                                    log.numeroAutorizacionAuto = responseAsnet.numeroAutorizacion;
                                }

                            }
                        }
                    }
                    else
                    {
                        log.CodigoRespuesta = responseCashOutModel.code;
                        log.DescripcionRespuesta = responseCashOutModel.message;
                        var descrip = string.Format("Ocurrio un error al realizar el retiro [{0} - {1}]", responseCashOutModel.code, responseCashOutModel.message);
                        logger.Info<string>(descrip);
                        respusta.Add("codigo", "01");
                        respusta.Add("descripcion", descrip);
                    }
                }
                else
                {
                    log.CodigoRespuesta = responseTRNCode.code;
                    log.DescripcionRespuesta = responseTRNCode.description;
                    var descrip = string.Format("Ocurrio un error al realizar el retiro de la transaccion [{0} - {1}]", responseTRNCode.code, responseTRNCode.description);
                    logger.Info<string>(descrip);
                    respusta.Add("codigo", "01");
                    respusta.Add("descripcion", descrip);
                }
                log.NumTransaccion = string.IsNullOrEmpty(responseTRNCode.data.code) ? "" : responseTRNCode.data.code;
                #endregion

                #region Old CashOut
                //CashResponseModel responseCashOut = await cashServices.ChashOut(requestCashOut);

                //if (responseCashOut.estado && responseCashOut.data.codRespuesta.Equals("0000"))
                //{
                //    Int64 ultimoCons = Int64.MinValue;
                //    String consecutivo = String.Empty;
                //    try
                //    {
                //        if (esPagoTercero)
                //        {
                //            consecutivo = getConsecutivoPago(idDeudor, out ultimoCons);
                //        }
                //        else
                //        {
                //            consecutivo = getConsecutivoPago(idCliente, out ultimoCons);
                //        }
                //    }
                //    catch (Exception ex)
                //    {
                //        logger.Error<Exception>(ex);
                //        log.CodigoRespuesta = "9014";
                //        log.DescripcionRespuesta = "Error al consultar el consecutivo de pago";
                //        respusta.Add("codigo", "03");
                //        respusta.Add("descripcion", String.Format("{0} - [{1}]", "Error al consultar el consecutivo", ex.Message));
                //    }
                //    if (!String.IsNullOrEmpty(consecutivo))
                //    {
                //        int inserto = 0;
                //        try
                //        {
                //            if (esPagoTercero)
                //            {
                //                inserto = insertaPago(idDeudor, monto, responseCashOut.data.nroTransaccion, consecutivo, numProducto);
                //            }
                //            else
                //            {
                //                inserto = insertaPago(idCliente, monto, responseCashOut.data.nroTransaccion, consecutivo, numProducto);
                //            }
                //        }
                //        catch (Exception ex)
                //        {
                //            logger.Error<Exception>(ex);
                //            log.CodigoRespuesta = "9004";
                //            log.DescripcionRespuesta = "Error al registrar el pago en tabla PHSPSE";
                //            respusta.Add("codigo", "02");
                //            respusta.Add("descripcion", String.Format("{0} - [{1}]", "Error al registrar el pago", ex.Message));
                //        }
                //        if (inserto > 0)
                //        {
                //            if (esPagoTercero)
                //            {
                //                actualizaConsecutivo(idDeudor, ultimoCons);
                //            }
                //            else
                //            {
                //                actualizaConsecutivo(idCliente, ultimoCons);
                //            }
                //            log.CodigoRespuesta = responseCashOut.data.codRespuesta;
                //            log.DescripcionRespuesta = responseCashOut.data.descRespuesta;
                //            respusta.Add("codigo", "00");
                //            respusta.Add("descripcion", "Transacción exitosa");
                //            ResponsePagoTarjetaCreditoDTO responseAsnet = null;

                //            if (esPagoTercero)
                //            {
                //                //responseAsnet = await AplicarPagoAutorizador(idDeudor, numProducto, montoAuth);
                //                responseAsnet = await PagoAutorizador(numProducto, idDeudor, tipoIdDeudor, monto.ToString());
                //            }
                //            else
                //            {
                //                //responseAsnet = await AplicarPagoAutorizador(idCliente, numProducto, montoAuth);
                //                responseAsnet = await PagoAutorizador(numProducto, idCliente, tipoIdDeudor, monto.ToString());
                //            }
                //            if (responseAsnet != null)
                //            {
                //                log.respuestaAuto = responseAsnet.codigoRespuesta;
                //                log.numeroAutorizacionAuto = responseAsnet.numeroAutorizacion;
                //            }

                //        }

                //    }
                //}
                //else
                //{
                //    log.CodigoRespuesta = responseCashOut.data.codRespuesta;
                //    log.DescripcionRespuesta = responseCashOut.data.descRespuesta;
                //    var descrip = String.Format("Ocurrio un error al realizar el retiro [{0} - {1}]", responseCashOut.data.codRespuesta, responseCashOut.data.descRespuesta);
                //    logger.Info<String>(descrip);
                //    respusta.Add("codigo", "01");
                //    respusta.Add("descripcion", descrip);
                //}
                //log.NumTransaccion = String.IsNullOrEmpty(responseCashOut.data.nroTransaccion) ? "" : responseCashOut.data.nroTransaccion;
                #endregion

            }
            catch (Exception ex)
            {
                logger.Error($"Error en RealizarPago {ex.ToString()}");
                respusta.Add("codigo", "06");
                respusta.Add("descripcion", "Ocurrió un error, intente mas tarde");
            }
            respusta.Add("data", JObject.FromObject(registrarLog(log)));
            //registrarLog(log);
            return respusta;
        }


        private async Task<ResponsePagoTarjetaCreditoDTO> AplicarPagoAutorizador(string idCliente, string tarjeta, string monto)
        {
            PagoTcoAsNet.RequestPagoTarjetaCreditoDTO request = new PagoTcoAsNet.RequestPagoTarjetaCreditoDTO();

            LogEventInfo logEventInfo = new LogEventInfo();
            logEventInfo.Level = NLog.LogLevel.Info;
            logEventInfo.Properties.Add("Input", new { idCliente, monto });

            DateTime feho = DateTime.Now;
            string fecha = feho.ToString("yyyyMMdd");
            string hora = feho.ToString("HHmmssfff");

            request.fechaNovedad = fecha;
            request.horaNovedad = hora;
            request.numeroIdentificacion = idCliente.PadRight(10);
            request.numeroTarjeta = tarjeta.PadRight(19);
            request.tipoProducto = tarjeta.StartsWith("8999") ? "40" : "31";
            monto = string.Format("{0}", monto);
            request.valorPago = monto.PadLeft(12, '0');

            try
            {
                PagoTarjetaCreditoImplServiceClient pago = new PagoTarjetaCreditoImplServiceClient("PagoTarjetaCreditoImplService");
                pago.Open();
                getPagoTarjetaCreditoResponse response = await pago.getPagoTarjetaCreditoAsync(request);

                if (response != null)
                {
                    ResponsePagoTarjetaCreditoDTO responsePago = response.Body.getPagoTarjetaCreditoReturn;

                    logEventInfo.Message = "Respuesta del servicio de pago de tarjeta del autorizador";
                    logEventInfo.Properties.Add("PagoTarjetaRespone", responsePago);
                    logger.Log(logEventInfo);
                    return response.Body.getPagoTarjetaCreditoReturn;
                }
            }
            catch (Exception ex)
            {
                logger.Error("Error AplicarPagoAutorizador" + ex.ToString());
                return null;
            }

            return null;
        }


        private string getConsecutivoPago(string idCliente, out long ultimoConse)
        {
            string consecutivo = string.Empty;
            ultimoConse = 0;
            long codigo;
            string connString = _configuration.GetConnectionString("FACT");
            try
            {
                using (DB2Connection conn = new DB2Connection(connString))
                {
                    using (DB2Command command = new DB2Command())
                    {
                        command.Connection = conn;
                        command.CommandText = "SELECT * FROM phsnitc WHERE C1NIT=@nit";
                        DB2Parameter param1 = new DB2Parameter();
                        param1.ParameterName = "@nit";
                        param1.Value = idCliente;
                        command.Parameters.Add(param1);
                        conn.Open();
                        using (DB2DataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                codigo = reader.GetInt64(1);
                                ultimoConse = reader.GetInt64(2);
                                consecutivo = string.Format("{0}{1}", codigo, (ultimoConse + 1).ToString("D6"));

                                //validar y ajustar en caso de ser necesario
                                while (ExisteConsecutivoPago(consecutivo))
                                {
                                    ultimoConse++;
                                    consecutivo = string.Format("{0}{1}", codigo, (ultimoConse + 1).ToString("D6"));
                                }

                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return consecutivo;
        }

        private bool ExisteConsecutivoPago(string ConsecutivoPhspse)
        {

            string connString = _configuration.GetConnectionString("FACT");
            try
            {
                using (DB2Connection conn = new DB2Connection(connString))
                {
                    using (DB2Command command = new DB2Command())
                    {
                        command.Connection = conn;
                        command.CommandText = "select c6ide from phspse where c6ide=@Consecutivo";
                        DB2Parameter param1 = new DB2Parameter();
                        param1.ParameterName = "@Consecutivo";
                        param1.Value = ConsecutivoPhspse;
                        command.Parameters.Add(param1);
                        conn.Open();
                        using (DB2DataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                //si existe
                                return true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return false;
        }

        private int insertaPago(string idCliente, decimal monto, string referencia, string consecutivo, string tarjeta)
        {
            int filasInsertadas;
            string connString = _configuration.GetConnectionString("FACT");
            try
            {
                using (DB2Connection conn = new DB2Connection(connString))
                {
                    using (DB2Command command = new DB2Command())
                    {
                        command.Connection = conn;
                        command.CommandText = @"INSERT INTO PHSPSE
                                                VALUES
                                                (@fehca,@tarjeta,@hora,@fecha1,@hora1,@conse,'03',@refe,@monto,@monto1,@nit)";
                        command.Parameters.Add("@fecha", DateTime.Now.ToString("yyyyMMdd"));
                        command.Parameters.Add("@hora", DateTime.Now.ToString("HHmmss"));
                        command.Parameters.Add("@tarjeta", tarjeta);
                        command.Parameters.Add("@fecha1", DateTime.Now.ToString("yyyyMMdd"));
                        command.Parameters.Add("@hora1", DateTime.Now.ToString("HHmmss"));
                        command.Parameters.Add("@conse", consecutivo);
                        command.Parameters.Add("@refe", string.Format("IBS {0}", referencia));
                        command.Parameters.Add("@monto", monto);
                        command.Parameters.Add("@monto1", monto);
                        command.Parameters.Add("@nit", idCliente);
                        conn.Open();
                        filasInsertadas = command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
            return filasInsertadas;
        }

        private void actualizaConsecutivo(string idCliente, long ultimoConse)
        {
            try
            {
                string connString = _configuration.GetConnectionString("FACT");
                using (DB2Connection conn = new DB2Connection(connString))
                {
                    using (DB2Command command = new DB2Command())
                    {
                        command.Connection = conn;
                        command.CommandText = @"Update phsnitc Set C1CON=@conse WHERE C1NIT=@nit";
                        command.Parameters.Add("@nit", idCliente);
                        command.Parameters.Add("@conse", ultimoConse + 1);
                        conn.Open();
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
        }

        private PagosEibsCms registrarLog(PagosEibsCms log)
        {
            try
            {

                Context.PagosEibsCms.Add(log);
                Context.SaveChanges();

            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
            return log;
        }


        private decimal ObtenerPagoTotal(string idCliente, string tarjeta)
        {
            decimal pagoTotal = decimal.MinValue;
            string connString = _configuration.GetConnectionString("FACT");
            try
            {
                using (DB2Connection conn = new DB2Connection(connString))
                {
                    using (DB2Command command = new DB2Command())
                    {
                        command.Connection = conn;
                        command.CommandText = @"select Sum(COALESCE(DYCXPM, 0))+H3R6VA as PAGO_TOTAL
                                            From phyesat
                                            LEFT JOIN phyesld ON DYNRTA = H3NRTA 
                                            where H3NRTA=@tarjeta AND H3UNNB=@idCliente
                                            Group by H3RXVA, H3R6VA";
                        DB2Parameter param1 = new DB2Parameter
                        {
                            ParameterName = "@tarjeta",
                            Value = tarjeta
                        };
                        command.Parameters.Add(param1);
                        DB2Parameter param2 = new DB2Parameter
                        {
                            ParameterName = "@idCliente",
                            Value = idCliente
                        };
                        command.Parameters.Add(param2);
                        conn.Open();
                        using (DB2DataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                pagoTotal = reader.GetDecimal(0);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
            return pagoTotal;
        }

        private bool PuedePagar(string numProducto, string idCliente, decimal monto)
        {
            LogEventInfo info = new LogEventInfo();
            info.Level = NLog.LogLevel.Info;
            info.Properties.Add("Monto", monto);
            decimal pagoHoy = decimal.Zero;
            decimal pagoTotal = ObtenerPagoTotal(idCliente, numProducto);
            info.Properties.Add("PagoTotal", pagoTotal);
            try
            {
                pagoHoy = Context.PagosEibsCms.Where(p => p.IdCliente.Equals(idCliente) &&
                                                          p.NumProducto.Equals(numProducto) &&
                                                          p.CodigoRespuesta.Equals("0000") &&
                                                          DbFunctions.TruncateTime(p.Fecha) == DbFunctions.TruncateTime(DateTime.Now))
                                              .Select(p => p.Monto)
                                              .DefaultIfEmpty(0)
                                              .Sum().Value;


                pagoHoy += monto;
                info.Properties.Add("PagoHoy", pagoHoy);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
            info.Properties.Add("PuedePagar", pagoHoy <= pagoTotal);
            logger.Log(info);
            return pagoHoy <= pagoTotal;
        }


        public JObject ConsultarPagoPorTarjeta(string tarjeta)
        {
            JObject dataPayment = new JObject();
            string connString = _configuration.GetConnectionString("FACT");
            try
            {
                using (DB2Connection conn = new DB2Connection(connString))
                {
                    using (DB2Command command = new DB2Command())
                    {
                        command.Connection = conn;
                        command.CommandText = @"SELECT  H3RXVA AS PAGOMINIMO, (COALESCE((SELECT SUM(LD.DYCXPM) FROM TCFILESER.PHYESLD LD WHERE LD.DYNRTA = E.H3NRTA),0) + E.H3R6VA ) AS PAGO_TOTAL,
                                                H3CDTI as TIPO_ID, H3UNNB as CEDULA, H3PZNB as NOMBRE
                                                FROM PHYESAT E
                                                INNER JOIN PHSCTAA P ON P.C1CTA = E.H3NRTA
                                                WHERE E.H3NRTA = @tarjeta AND (h3uenb not like '%2' and h3uenb not like '%1' and h3uenb not like  '3%')";
                        DB2Parameter param1 = new DB2Parameter
                        {
                            ParameterName = "@tarjeta",
                            Value = tarjeta
                        };
                        command.Parameters.Add(param1);
                        conn.Open();
                        using (DB2DataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                dataPayment.Add("codigo", "00");
                                dataPayment.Add("descripcion", "Consulta exitosa");
                                dataPayment.Add("pagoMinimo", reader.GetString(0).ToString());
                                dataPayment.Add("pagoTotal", reader.GetString(1).ToString());
                                dataPayment.Add("tipoId", reader.GetString(2));
                                dataPayment.Add("idCliente", reader.GetString(3));
                                dataPayment.Add("nombre", reader.GetString(4).Trim());
                                dataPayment.Add("valorCuotaCorriente", reader.GetString(0).ToString());
                                dataPayment.Add("valorCuotasVencidas", "0");
                            }
                            else
                            {
                                dataPayment.Add("codigo", "01");
                                dataPayment.Add("descripcion", "No se encontró información o estado de tarjeta inválido.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                dataPayment.Add("codigo", "02");
                dataPayment.Add("descripcion", "Ocurrió un error al realizar la consulta ");
            }
            return dataPayment;
        }


        public async Task<JObject> ConsultarPagoPorPrestamo(string numPrestamo)
        {
            JObject dataPayment = new JObject();
            try
            {
                using (var httpClient = new HttpClient())
                {
                    Uri urlResumenCms = new Uri(string.Format("{0}{1}", _configuration["Url_Consulta_Prestamo"], numPrestamo));
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    var response = await httpClient.GetAsync(urlResumenCms);
                    JObject resultado = await response.Content.ReadAsAsync<JObject>();
                    if (resultado != null)
                    {
                        var estado = resultado["estado"].Value<bool>();
                        if (estado)
                        {
                            var cuenta = resultado["data"]["cuenta"].Value<JObject>();
                            var valorCuotaCorriente = cuenta.Value<decimal>("valorCuotaCorriente");
                            var valorCuotasVencidas = cuenta.Value<decimal>("valorCuotasVencidas");
                            var pagoMinimo = valorCuotasVencidas == 0 ? valorCuotaCorriente : valorCuotasVencidas;
                            dataPayment.Add("codigo", "00");
                            dataPayment.Add("descripcion", "Consulta exitosa");
                            dataPayment.Add("pagoMinimo", pagoMinimo.ToString());
                            dataPayment.Add("pagoTotal", cuenta["pagoTotal"].Value<decimal>().ToString());
                            dataPayment.Add("tipoId", SerfinanzaUtils.HomologarTipoId(cuenta.GetValue("tipoIdCliente").Value<string>(), SerfinanzaUtils.Sistema.CMS));
                            dataPayment.Add("idCliente", cuenta["idCliente"].Value<string>());
                            dataPayment.Add("nombre", cuenta["nombreCompleto"].Value<string>().Trim());
                            dataPayment.Add("saldoCapital", cuenta["saldoCapital"].Value<decimal>().ToString());
                            dataPayment.Add("vencimientoCuota", cuenta["vencimientoCuota"].Value<string>().ToString());
                            dataPayment.Add("valorDesembolso", cuenta["valorDesembolso"].Value<decimal>().ToString());
                            dataPayment.Add("fechaVencimiento", cuenta["fechaVencimiento"].Value<string>().ToString());
                            dataPayment.Add("valorCuotaCorriente", cuenta["valorCuotaCorriente"].Value<decimal>().ToString());
                            dataPayment.Add("valorCuotasVencidas", cuenta["valorCuotasVencidas"].Value<decimal>().ToString());
                        }
                        else
                        {
                            logger.Info(resultado.ToString());
                            dataPayment.Add("codigo", "01");
                            dataPayment.Add("descripcion", "No se encontró información");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                dataPayment.Add("codigo", "02");
                dataPayment.Add("descripcion", "Ocurrió un error al realizar la consulta ");
            }

            return dataPayment;
        }


        public async Task<JObject> RealizarPagoPrestamo(JObject parameters)
        {
            JObject Result = new JObject();
            using (var httpClient = new HttpClient())
            {
                string urlPagoPrestamos = _configuration["Url_Pago_Prestamos"];
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                HttpContent content = new StringContent(parameters.ToString());
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                var response = await httpClient.PostAsync(urlPagoPrestamos, content);
                JObject resultado = await response.Content.ReadAsAsync<JObject>();
                bool estadoRespuesta = resultado.GetValue("estado").Value<bool>();
                JObject datosRespuesta = resultado.GetValue("data").Value<JObject>();
                var descRespuesta = resultado["data"]["descRespuesta"].Value<string>();
                if (estadoRespuesta)
                {

                    var codRespuesta = resultado["data"]["codRespuesta"].Value<string>();

                    if (codRespuesta.Equals("0000"))
                    {

                        Result.Add("codigo", "00");
                        Result.Add("descripcion", "Transacción Exitosa");
                        Result.Add("data", datosRespuesta["nroTransaccion"].Value<string>());
                    }
                    else
                    {
                        Result.Add("codigo", "01");
                        Result.Add("descripcion", descRespuesta);
                        Result.Add("data", datosRespuesta);
                    }
                }
                else
                {
                    Result = new JObject();
                    Result.Add("codigo", "02");
                    Result.Add("descripcion", descRespuesta);
                    Result.Add("data", descRespuesta);
                }
                LogEventInfo logEventInfo = new LogEventInfo();
                logEventInfo.Level = NLog.LogLevel.Info;
                logEventInfo.Properties.Add("Input", parameters.ToString());
                logEventInfo.Properties.Add("Respuesta", resultado.ToString());
                logger.Log(logEventInfo);

            }
            return Result;
        }

        public async Task<ResponsePagoTarjetaCreditoDTO> PagoAutorizador(string numTarjeta, string idCliente, string tipoId, string monto)
        {
            ResponsePagoTarjetaCreditoDTO respAsNet = null;
            try
            {
                string montoAuth = string.Format("{0:0.##}", monto);
                if (montoAuth.Split(',').Length == 1)
                {
                    montoAuth = string.Format("{0}00", montoAuth);
                }
                montoAuth = montoAuth.Replace(",", string.Empty);
                Parametros param;


                param = await Context.Parametros.Where(a => a.DescripcionParametro == "DiasMora" && a.Sistema == "PAGOENLINEA").FirstOrDefaultAsync();

                var responseAsnet = AplicarPagoAutorizador(idCliente, numTarjeta, montoAuth);
                var consulta = ConsultarTarjeta(numTarjeta);
                var detalleCMS = _producServices.DetalleCMS(tipoId, idCliente, numTarjeta);

                await Task.WhenAll(consulta, detalleCMS, responseAsnet);

                var (mora, diasMora) = consulta.Result;
                var detCMS = detalleCMS.Result;
                respAsNet = responseAsnet.Result;
                decimal diasMoraParam = decimal.Parse(param.ValorParametro);

                if (respAsNet != null && detCMS != null)
                {
                    string strPagMin = detCMS.GetValue("pagominimoVencido").Value<string>();
                    decimal pagoMinimo = decimal.Parse(strPagMin);
                    decimal montoApagar = decimal.Parse(monto);
                    if (mora && diasMora <= diasMoraParam && montoApagar >= pagoMinimo && respAsNet.codigoRespuesta.Equals("OK000"))
                    {
                        #region Actualizar a estado Normal
                        NovedadesBloqueoSR.NovedadesNoMon_WSClient client = new NovedadesBloqueoSR.NovedadesNoMon_WSClient();
                        NovedadesBloqueoSR.NovedadNoMon_Dto novedad = new NovedadesBloqueoSR.NovedadNoMon_Dto();
                        novedad.codigoCanal = "0194";
                        novedad.codigoEntidad = "0423";
                        novedad.bin = numTarjeta.Substring(0, 6);
                        novedad.tipoNovedad = "05";
                        novedad.numeroTarjetaAsignado = numTarjeta;
                        novedad.tipoIdentificacion = tipoId;
                        novedad.numeroIdentificacion = idCliente;
                        novedad.tipoCuenta = "31";
                        novedad.nit = "860043186";
                        string usuario = _configuration["usuarioAutorizdor"];
                        string pass = _configuration["claveAutorizador"];


                        var respuestaNovedad = await client.aplicarNovedadAsync(novedad, usuario, pass);

                        if (respuestaNovedad.Body.aplicarNovedadReturn.codigoRespuesta.Equals("OK000"))
                        {
                            await ActualizarEstadoCMS("0", numTarjeta);
                        }

                        logger.Info($"Respuesta Aplicar Novedad Desbloqueo Mora Autorizador Idcliente={idCliente} Request {JObject.FromObject(novedad).ToString()} respuesta {JObject.FromObject(respuestaNovedad).ToString()}");
                        #endregion
                    }
                    else
                    {
                        logger.Info($"Cliente no cumple con las condiciones para desbloqueo por mora Idcliente={idCliente}");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error($"No se puede aplicar pago autorizador - validar mora Idcliente= {idCliente} " + ex.ToString());
            }
            return respAsNet;
        }

        public async Task<(bool, decimal)> ConsultarTarjeta(string numTarjeta)
        {
            bool mora = false;
            decimal Diasmora = decimal.Zero;
            try
            {
                string connString = _configuration.GetConnectionString("FACT");
                using (DB2Connection conn = new DB2Connection(connString))
                {
                    using (DB2Command command = new DB2Command())
                    {
                        command.Connection = conn;
                        command.CommandText = string.Format("SELECT H3UENB, H3SIVA FROM PHYESAT WHERE H3NRTA ='{0}'", numTarjeta);
                        conn.Open();
                        using (DB2DataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string estado = reader.GetValue(0).ToString().Trim();
                                mora = estado.Equals("100000");
                                Diasmora = decimal.Parse(reader.GetValue(1).ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                mora = false;
                Diasmora = decimal.Zero;
                logger.Error(ex);
            }
            return (mora, Diasmora);
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
                            query = string.Format(@"INSERT INTO PHYNOVE (HCTPNB, HCBJNB, HCFNCD, HCTQNB, HCJCCD, HCUDNB, HCS4NB, HCL6TX, HCBKNB,HCKXTX, HCQ9NB, HCRANB, HCCDOF, HCJLT1) VALUES ('{0}', {2}, '480', '8', '01', {2},{3}, 'SERVICE', {2}, 'D', {1}, 0, '0001', '08001')", tarjeta, "100000", thisDate.ToString("yyyyMMdd"), HoraFormat);
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
    }

}
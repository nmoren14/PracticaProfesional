using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using System.Data;
using System.Data.Entity;
using System.Globalization;
using IBM.Data.DB2.Core;
using BancaServices.Models;
using BancaServices.Domain.Interfaces;
using BancaServices;
using BancaServices.Application.Services.SerfiUtils;

namespace BancaServices.Application.Services
{
    public class ClienteServices : IClienteServices
    {
        private readonly NLog.ILogger logger = LogManager.GetCurrentClassLogger();
        private readonly HttpClient client;
        private readonly IProductosService productosService;
        private readonly IConfiguration _configuration;
        private readonly BancaServicesLogsEntities Context; // Inject the DbContext


        public ClienteServices(IProductosService _productosService, BancaServicesLogsEntities dbContext, IConfiguration configuration)
        {
            client = new HttpClient();
            productosService = _productosService;
            _configuration = configuration;
            Context = dbContext; // Assign the injected DbContext
        }

        public Cliente ConsultaClienteByDoc(string tipoId, string idCliente)
        {
            string connString = _configuration.GetConnectionString("FACT");
            DB2DataReader reader;

            try
            {
                using (DB2Connection conn = new DB2Connection(connString))
                {
                    string query = @"SELECT TCCDTI, TCUNNB, TCS9TX, TCJOT1, TCJPT1, JRPZNB, TCPWNB, TCCDSE, TCCDEC, TCT3NB, TCCDTR, TCQRTX, TCJHCD, JRNATX, JRT1NB, O5TRT1, TCCDPF, TCCDAE, TCQSTX, TCQUTX, TCX2NB, TCQTTX, TCJICD, TCYANB, TCPEVA, TCQKVA, TCJRV1, TCJSV1, TCQMVA, TCQNVA, TCMON1, TCJTT1, TCMLN1, TCEDTX, TCTRP1, TCJJCD, TCQVTX, TCQPVA, TCPLVE, TCQQVA, (SELECT PH.H3NATX FROM PHYESAT PH WHERE H3UNNB = @doc2 AND H3CDTI = @tdoc2 order by PH.H3UENB limit 1) 
                                               FROM (SELECT DISTINCT TCCDTI, TCUNNB, TCS9TX, TCJOT1, TCJPT1, JRPZNB, TCPWNB, TCCDSE, TCCDEC, TCT3NB, TCCDTR, TCQRTX, TCJHCD, JRNATX, JRT1NB, (SELECT MAX(O5TRT1) FROM PHYDITA WHERE O5NCST='G' AND O5TRT1 LIKE '%@%' AND O5TUT1='    ' || RIGHT(REPEAT('0', 12) || LTRIM(JRU7NB), 12)) O5TRT1, TCCDPF, TCCDAE, TCQSTX, TCQUTX, TCX2NB, TCQTTX, TCJICD, TCYANB, TCPEVA, TCQKVA, TCJRV1, TCJSV1, TCQMVA, TCQNVA, TCMON1, TCJTT1, TCMLN1, TCEDTX, TCTRP1, TCJJCD, TCQVTX, TCQPVA, TCPLVE, TCQQVA,(SELECT MAX(JRU9NB) FROM PHYSOLI S1 WHERE S1.JRU7NB=S.JRU7NB) FECHA, JRU9NB
                                               FROM PHYSOLI S, PHYCLIE WHERE TCUNNB=JRU7NB) Q WHERE Q.JRU9NB=FECHA AND TCCDTI=@tdoc  AND TCUNNB= @doc";
                    conn.Open();

                    using (DB2Command command = new DB2Command(query, conn))
                    {
                        command.CommandType = CommandType.Text;
                        command.Parameters.Add(new DB2Parameter("@doc", DB2Type.VarChar));
                        command.Parameters["@doc"].Value = idCliente;
                        command.Parameters.Add(new DB2Parameter("@tdoc", DB2Type.VarChar));
                        command.Parameters["@tdoc"].Value = tipoId;
                        command.Parameters.Add(new DB2Parameter("@doc2", DB2Type.VarChar));
                        command.Parameters["@doc2"].Value = idCliente;
                        command.Parameters.Add(new DB2Parameter("@tdoc2", DB2Type.VarChar));
                        command.Parameters["@tdoc2"].Value = tipoId;
                        reader = command.ExecuteReader();

                        if (reader.Read())
                        {
                            #region Asignacion
                            Cliente cliente = new Cliente();
                            cliente.Codigo = "00";
                            cliente.Descripcion = "Cliente existe en serfinanza";
                            cliente.PrimerNombre = reader["TCS9TX"].ToString().Trim();
                            cliente.SegundoNombre = "";
                            cliente.PrimerApellido = reader["TCJOT1"].ToString().Trim();
                            cliente.SegundoApellido = reader["TCJPT1"].ToString().Trim();
                            cliente.PaisNacimiento = "";
                            cliente.EstadoCivil = reader["TCCDEC"].ToString().Trim();
                            cliente.Ocupacion = reader["TCCDAE"].ToString().Trim();
                            cliente.Estudios = "";
                            cliente.Profesion = reader["TCCDPF"].ToString().ToString();
                            cliente.ActividadEconomica = reader["TCCDAE"].ToString().Trim();
                            cliente.DireccionResidencia = reader["H3NATX"].ToString().Trim();
                            cliente.BarrioResidencia = "";
                            cliente.CiudadResidencia = reader["TCJHCD"].ToString().Trim();
                            cliente.DepartamentoResidencia = "";
                            cliente.PaisResidencia = "";
                            cliente.TelefonoPrincipal = reader["JRT1NB"].ToString().Trim();
                            cliente.CorreoElectronico = reader["O5TRT1"].ToString().Trim();
                            cliente.NombreEmpresa = reader["TCQUTX"].ToString().Trim();
                            cliente.DireccionEmpresa = reader["TCQTTX"].ToString().Trim();
                            cliente.TelefonoEmpresa = reader["TCX2NB"].ToString().Trim();
                            cliente.Ingresos = reader["TCPEVA"].ToString().Trim();
                            cliente.Egresos = reader["TCJRV1"].ToString().Trim();
                            cliente.TotalActivo = reader["TCQMVA"].ToString().Trim();
                            cliente.TotalPasivo = reader["TCQNVA"].ToString().Trim();
                            cliente.OperMonedaExt = "";
                            cliente.EstratoVivienda = "";
                            cliente.PersonasACargo = reader["TCT3NB"].ToString().Trim();
                            cliente.PaisLabora = "";
                            cliente.DepartamentoLabora = "";
                            cliente.CiudadLabora = "";
                            cliente.Cargo = reader["TCQSTX"].ToString().Trim();
                            cliente.FechaIngreso = reader["TCYANB"].ToString().Trim();
                            cliente.Salario = reader["TCPEVA"].ToString().Trim();
                            cliente.OtrosIngresos = reader["TCQKVA"].ToString().Trim();
                            cliente.DetalleOtrosIngresos = "";
                            cliente.TituloPregrado = "";
                            cliente.Patrimonio = "";
                            cliente.Celular = reader["JRT1NB"].ToString().Trim();
                            cliente.Estado = "ACTIVO CMS";
                            return cliente;
                            #endregion
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return null;
            }

            return null;
        }

        public async Task<Cliente> ConsultaClienteEIBS(string tipoId, string idCliente)
        {
            Cliente cliente = null;

            JObject data = new JObject
            {
                { "tipoId", tipoId },
                { "idCliente", idCliente }
            };

            try
            {
                string url = _configuration["Url_DatosBasicos"];
                JObject res = await ConsumirApiRest.ConsumirApiSalidaJObject(url, data, logger: logger);
                if (res != null && res.ContainsKey("data") && res["data"] is JObject)
                {
                    string codigo = res.SelectToken("data.codigoRespuesta").Value<string>();
                    if (codigo != "00") return null;
                    cliente = JsonConvert.DeserializeObject<Cliente>(res.Value<JObject>("data").ToString());
                    cliente.Codigo = "00";
                    cliente.Descripcion = "Cliente existe en serfinanza";
                }

            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
            return cliente;
        }
        public async Task<Cliente> ConsultaClienteByDocIVR(string tipoId, string idCliente)
        {
            try
            {
                var Ibs = ConsultaClienteEIBS(tipoId, idCliente);
                await Task.WhenAll(Ibs);
                Cliente cliente = Ibs.Result;
                if (cliente == null)
                {
                    cliente = ConsultaClienteByDoc(tipoId, idCliente);
                }
                return cliente;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
            return null;
        }
        private async Task<bool> ConsultasPerGracia(JObject item)
        {
            try
            {
                string tarjeta = item.GetValue("numProducto").Value<string>();
                List<ValidacioneRediferidos> validaciones = await Context.ValidacioneRediferidos.AsNoTracking().ToListAsync();
                string PeriodoGracia = validaciones.Where(b => b.Id == 1).FirstOrDefault().Valor;
                int DiasMoraMax = int.Parse(validaciones.Where(b => b.Id == 4).FirstOrDefault().Valor);
                string connString = _configuration.GetConnectionString("FACT");
                string sql = string.Format("SELECT DYGVC1 AS GRACIA  FROM PHYESLD WHERE DYCXPM >= 0 AND DYNRTA = '{0}'", tarjeta);
                string query = string.Format("SELECT H3SIVA AS DIAS_MORA, H3RXVA AS PAGO_MINIMO FROM PHYESAT WHERE H3NRTA = '{0}'", tarjeta);
                using (DB2Connection conn = new DB2Connection(connString))
                {
                    conn.Open();

                    using (DB2Command command = new DB2Command(sql, conn))
                    {
                        command.CommandType = CommandType.Text;
                        var reader = await command.ExecuteReaderAsync();
                        Console.WriteLine(reader.GetName(0) + " ");
                        string nombreColumna = reader.GetName(0);
                        while (reader.Read())
                        {
                            if (reader.GetName(0).Equals("GRACIA"))
                            {
                                string Pgracia = reader.GetString(0);
                                if (Pgracia.Equals(PeriodoGracia))
                                {
                                    //periodo de gracia en dias de mora > 60 retorna false
                                    using (DB2Command cmd = new DB2Command(query, conn))
                                    {
                                        cmd.CommandType = CommandType.Text;
                                        var rd = await cmd.ExecuteReaderAsync();
                                        while (rd.Read())
                                        {
                                            decimal mora = rd.GetDecimal(0);
                                            if (mora > DiasMoraMax) { return false; }
                                        }
                                    }
                                    return true;
                                }
                            }
                        }
                        reader.Close();
                    }
                    conn.Close();
                }
            }
            catch (Exception)
            {
                return false;
            }
            return false;
        }
        private async Task<bool> Consultas(JObject item, string tipoId, string idCliente)
        {
            try
            {
                if (!item.GetValue("estado").Value<string>().ToUpper().Equals("NORMAL")) return false;
                string tarjeta = item.GetValue("numProducto").Value<string>();
                List<ValidacioneRediferidos> validaciones = await Context.ValidacioneRediferidos.AsNoTracking().ToListAsync();
                int MesesRediferir = int.Parse(validaciones.Where(b => b.Id == 2).FirstOrDefault().Valor);
                string PeriodoGracia = validaciones.Where(b => b.Id == 1).FirstOrDefault().Valor;
                int SaldoCapitalMax = int.Parse(validaciones.Where(b => b.Id == 3).FirstOrDefault().Valor);
                int DiasMoraMax = int.Parse(validaciones.Where(b => b.Id == 4).FirstOrDefault().Valor);
                int PagoMinimo = int.Parse(validaciones.Where(b => b.Id == 5).FirstOrDefault().Valor);
                int MesesApertura = int.Parse(validaciones.Where(b => b.Id == 6).FirstOrDefault().Valor);
                int SaldoPeriodoGracia = int.Parse(validaciones.Where(b => b.Id == 7).FirstOrDefault().Valor);
                string Calificacion = validaciones.Where(b => b.Id == 8).FirstOrDefault().Valor;
                List<string> listaCalificacion = null;
                listaCalificacion = Calificacion.Split(',').ToList();
                string GrupoPeriodoGracia = validaciones.Where(b => b.Id == 9).FirstOrDefault().Valor;
                CalificacionClientes calificacion = Context.CalificacionClientes.AsNoTracking().Where(a => a.IdCliente.Equals(idCliente) && a.TipoId.Equals(tipoId)).Where(b => b.Cuenta.Equals(tarjeta)).FirstOrDefault();
                if (calificacion != null)
                {
                    if (listaCalificacion.Where(x => x.Contains(calificacion.Calificacion)).Any()) { return false; }
                }
                //else
                //{
                //    return false;
                //}

                string connString = _configuration.GetConnectionString("FACT");

                DateTime fechaExpedicion = DateTime.Parse(item.GetValue("fechaEmision").Value<string>());
                if (fechaExpedicion > DateTime.Now.AddMonths(MesesApertura)) return false;

                List<string> querys = new List<string>();
                querys.Add(string.Format("SELECT DYGVC1 AS GRACIA  FROM PHYESLD WHERE DYCXPM >=0 AND DYNRTA = '{0}'", tarjeta));
                querys.Add(string.Format("SELECT C5CDTT AS TRANSACCION, C5BYC1 AS MOTIVO, C5SINB AS DATE_REDIFERIR FROM PHYHTRA WHERE C5NRTA = '{0}' AND C5CDTT = 'PR' AND C5SINB = (SELECT MAX(C5SINB) FROM PHYHTRA WHERE C5NRTA = '{0}' AND C5CDTT = 'PR')", tarjeta));
                querys.Add(string.Format("SELECT(SELECT(SELECT COALESCE(SUM(DYCXPM),0) FROM PHYESLD WHERE DYNRTA = '{0}') + H3R6VA FROM PHYESAT WHERE H3NRTA = '{0}') - H3PPVA - H3PQVA - H3PRVA - H3RCVA - H3RDVA - H3REVA - H3RFVA - H3E9VA - H3CCC1 - H3FBVA - H3SSDQ - H3SREA - H3DEAR - H3EQWR - H3RRSA - H3RGVA - (SELECT QGCAPA - QGCPMA FROM PHYEXEA WHERE QGNRTA = '{0}') AS SALDO_CAPITAL FROM PHYESAT WHERE H3NRTA = '{0}'", tarjeta));
                querys.Add(string.Format("SELECT H3SIVA AS DIAS_MORA, H3RXVA AS PAGO_MINIMO FROM PHYESAT WHERE H3NRTA = '{0}'", tarjeta));
                foreach (string query in querys)
                {
                    using (DB2Connection conn = new DB2Connection(connString))
                    {
                        conn.Open();

                        using (DB2Command command = new DB2Command(query, conn))
                        {
                            command.CommandType = CommandType.Text;
                            var reader = await command.ExecuteReaderAsync();
                            Console.WriteLine(reader.GetName(0) + " ");
                            string nombreColumna = reader.GetName(0);
                            while (reader.Read())
                            {
                                if (reader.GetName(0).Equals("GRACIA"))
                                {
                                    string Pgracia = reader.GetString(0);
                                    if (Pgracia.Equals(PeriodoGracia))
                                    {
                                        bool consulta = ConsultarCliente(tipoId, idCliente, tarjeta, GrupoPeriodoGracia);
                                        if (consulta)
                                        {
                                            return false;
                                        }
                                    }
                                }
                                else if (reader.GetName(0).Equals("TRANSACCION"))
                                {
                                    if (reader.GetString(0).Equals("PR"))
                                    {
                                        DateTime fechaRediferir = DateTime.ParseExact(reader.GetString(2), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None);
                                        if (fechaRediferir >= DateTime.Now.AddMonths(MesesRediferir)) return false;
                                    }
                                }
                                else if (reader.GetName(0).Equals("SALDO_CAPITAL"))
                                {
                                    decimal SaldoCapital = reader.GetDecimal(0);
                                    if (SaldoCapital < SaldoCapitalMax) return false;
                                }
                                else if (reader.GetName(0).Equals("DIAS_MORA"))
                                {
                                    decimal diasMora = reader.GetDecimal(0);
                                    decimal minPago = reader.GetDecimal(1);
                                    if (diasMora > DiasMoraMax) return false;
                                    if (minPago < PagoMinimo) return false;
                                }
                            }
                            reader.Close();
                        }
                        conn.Close();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }
        private bool ConsultarCliente(string tipoId, string idCliente, string numCuenta, string grupo)
        {
            ClientesPeriodoGracia cliente = Context.ClientesPeriodoGracia.AsEnumerable().Where(a => a.TipoId == tipoId && a.IdCliente == idCliente).Where(b => b.Grupo.Equals(grupo)).Where(c => c.Cuenta.Equals(numCuenta)).LastOrDefault();
            if (cliente != null)
            {
                return true;
            }
            return false;
        }
        public async Task<JArray> apps(string tipoId, string idCliente)
        {
            JArray productosFinal = null;
            try
            {
                JArray productos = await productosService.ConsultaCMS(tipoId, idCliente, true);
                if (productos.Count > 0)
                {
                    productosFinal = new JArray();
                    foreach (JObject item in productos)
                    {
                        try
                        {
                            string tipoProducto = item.GetValue("tipoProducto").Value<string>();
                            string estado = item.GetValue("estado").Value<string>();
                            if (tipoProducto.Equals("04")/* && estado.ToUpper().Equals("NORMAL")*/)
                            {
                                productosFinal.Add(item);
                            }
                        }
                        catch (Exception)
                        {

                        }

                    }
                }

            }
            catch (Exception ex)
            {
                return null;
            }
            return productosFinal;
        }
        public async Task<ClienteDet> ObtenerDetalleCliente(string tipoId, string idCliente)
        {
            ClienteDet clienteDet = new ClienteDet();
            try
            {
                var Resumen = apps(tipoId, idCliente);
                bool CumpleRediferir = false;
                bool perGracia = false;
                await Task.WhenAll(Resumen);
                JArray productos = Resumen.Result;

                if (productos != null)
                {
                    clienteDet.Codigo = "00";
                    clienteDet.Descripcion = "Exitoso";
                    foreach (JObject item in productos)
                    {
                        #region Ciclo Cumple Rediferir
                        try
                        {
                            CumpleRediferir = await Consultas(item, tipoId, idCliente);
                            if (CumpleRediferir)
                            {
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                        }
                        #endregion
                    }

                    foreach (JObject item in productos)
                    {
                        #region Ciclo Periodo Gracia
                        try
                        {
                            perGracia = await ConsultasPerGracia(item);
                            if (perGracia)
                            {
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                        }
                        #endregion
                    }
                }
                else
                {
                    clienteDet.Codigo = "01";
                    clienteDet.Descripcion = "¡Opps!, Ha ocurrido un error al consultar los productos";
                }
                clienteDet.CumpleRediferir = CumpleRediferir;
                clienteDet.perGracia = perGracia;
                return clienteDet;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}
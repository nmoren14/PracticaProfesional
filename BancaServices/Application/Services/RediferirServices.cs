using BancaServices.Domain.Interfaces;
using BancaServices;
using IBM.Data.DB2.Core;
using Newtonsoft.Json.Linq;
using System.Data.Entity.Validation;
using System.Globalization;
using System.Text;

namespace BancaServices.Application.Services
{
    public class RediferirServices : IRediferirServices
    {
        private readonly IConfiguration _configuration;
        private readonly BancaServicesLogsEntities Context; // Inject the DbContext

        public RediferirServices(IConfiguration configuration, BancaServicesLogsEntities dbContext)
        {
            _configuration = configuration;
            Context = dbContext; // Assign the injected DbContext
        }
        public async Task<JObject> ValidarConsultaCMS(JArray listaCms, string TipoId, string IdCliente)
        {
            JObject respuesta = new JObject();
            try
            {
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
                        if (categoria == 4)
                        {
                            referencias = new JObject();
                            condiciones = new JObject();
                            condiciones = ValidarCondiciones(productoCms.GetValue("numProducto").Value<string>(), TipoId, IdCliente);
                            bool apto = condiciones.GetValue("Respuesta").Value<bool>();
                            if (apto)
                            {
                                referencias.Add("numProducto", productoCms.GetValue("numProducto").Value<string>());
                                referencias.Add("nomProducto", productoCms.GetValue("nomProducto").Value<string>());
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
            string monto = "";
            string plazo = "";
            try
            {
                string CardNumber = data.GetValue("CardNumber").Value<string>();
                monto = decimal.Round(ConsultarMonto(CardNumber), 0).ToString();
                plazo = data.GetValue("Plazo").Value<string>();
                string NuevaCuota = decimal.Round(CalcularCuota(CardNumber, plazo), 0).ToString();
                result.Add("Codigo", "01");
                result.Add("CardNumber", CardNumber.Substring(12));
                result.Add("NuevaCuota", NuevaCuota);
                result.Add("Plazo", plazo);
                result.Add("Tasa", ConsultarTasa().ToString());
                //Model
                LogModel.TipoId = ConsultarTipoID(CardNumber);
                LogModel.IdCliente = ConsultarCedula(CardNumber);
                LogModel.Monto = monto;
                LogModel.NumTarjeta = CardNumber.Substring(12);
                LogModel.IpCliente = data.GetValue("Ip").Value<string>();
                LogModel.FechaRegistro = DateTime.Now;
                LogModel.Plazo = int.Parse(plazo);
                LogModel.Reestructurado = ConsultarReestructurado(CardNumber);
                if (string.IsNullOrEmpty(LogModel.Reestructurado))
                {
                    result.Add("Descripcion", "Exitoso");
                }
                else
                {
                    result.Add("Descripcion", "Solicitud exitosa, sujeta a aprobación");
                }
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
            GuardarTransaccion(LogModel);
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
                monto = decimal.Round(ConsultarMonto(CardNumber), 0).ToString();
                plazo = data.GetValue("Plazo").Value<string>();
                tasa = ConsultarTasa().ToString();
                string NuevaCuota = decimal.Round(CalcularCuota(CardNumber, plazo), 0).ToString();
                result.Add("Codigo", "01");
                result.Add("Descripcion", "Exitoso");
                result.Add("CardNumber", CardNumber.Substring(12));
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

        #region MetodosDelServicio
        public JObject ValidarCondiciones(string CardNumber, string TipoId, string IdCliente)
        {
            JObject result = new JObject();
            bool cumple = true;
            try
            {
                //Consultas respectivas

                decimal monto = ConsultarMonto(CardNumber);
                DateTime FechaApertura = DateTime.ParseExact(ConsultarFechaApertura(CardNumber), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None);
                bool Bloqueado = ConsultarBloqueoTarjeta(CardNumber);
                decimal DiasMora = ConsultarMoraTarjeta(CardNumber);
                string Tasa = ConsultarTasa().ToString();
                bool Solicitud = ConsultarSolicitud(CardNumber, TipoId, IdCliente);
                //Validaciones
                if (monto < 50000) { cumple = false; }
                if (FechaApertura > DateTime.Now.AddMonths(-3)) { cumple = false; }
                if (Bloqueado) { cumple = false; }
                if (DiasMora > 30) { cumple = false; }
                if (Solicitud) { cumple = false; }
                if (cumple)
                {
                    result.Add("Codigo", "01");
                    result.Add("Descipcion", "El cliente con la tarjeta terminada en " + CardNumber.Substring(12) + " cumple con los requisitos para rediferir sus cuotas");
                    result.Add("Respuesta", true);
                    result.Add("Monto", monto.ToString());
                    result.Add("Tasa", Tasa + "%");
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
        public decimal CalcularCuota(string CardNumber, string Cplazo)
        {
            decimal cuota = 0;
            double monto = Convert.ToDouble(ConsultarMonto(CardNumber));
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
        private bool ConsultarSolicitud(string CardNumber, string TipoId, string IdCliente)
        {
            try
            {
                int Tipo_Id = int.Parse(TipoId);
                long Id_Cliente = long.Parse(IdCliente);
                string NumTarjeta = CardNumber.Substring(12);
                int DiasEsperaSolicitud = int.Parse(_configuration["MesesEsperaSolicitarRediferido"]);
                List<RediferidosLog> redi = Context.RediferidosLogs.Where(a => a.TipoId == Tipo_Id && a.IdCliente == Id_Cliente && a.NumTarjeta == NumTarjeta).ToList();
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
        private void GuardarTransaccion(RediferidosLog transaccion)
        {
            try
            {


                Context.RediferidosLogs.Add(transaccion);
                Context.SaveChanges();

            }
            catch (DbEntityValidationException dbEx)
            {
                foreach (var errors in dbEx.EntityValidationErrors)
                {

                }
            }
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
        private decimal ConsultarMonto(string CardNumber)
        {
            decimal monto = 0;
            try
            {
                string ConString = _configuration.GetConnectionString("FACT");
                string query = GetQueryConsultarMonto(CardNumber);
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
        private decimal ConsultarMoraTarjeta(string CardNumber)
        {
            decimal DiasMora = 0;
            try
            {
                string ConString = _configuration.GetConnectionString("FACT");
                string query = GetQueryConsultarDiasMora(CardNumber);
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
        private string GetQueryConsultarReestructurado(string Card)
        {
            return string.Format("SELECT MAX(C5FFPL) FROM PHYHTRA WHERE C5NRTA='{0}' AND C5CDTT = 'PR'", Card);
        }
        private string GetQueryConsultarDiasMora(string Card)
        {
            return string.Format("SELECT H3SIVA FROM PHYESAT WHERE H3NRTA = '{0}'", Card);
        }
        private string GetQueryConsultarTipoID(string Card)
        {
            return string.Format("SELECT H3CDTI FROM PHYESAT WHERE H3NRTA = '{0}'", Card);
        }
        private string GetQueryConsultarBloqueo(string Card)
        {
            return string.Format("SELECT H3UENB FROM PHYESAT WHERE H3NRTA = '{0}'", Card);
        }
        private string GetQueryConsultarMonto(string Card)
        {
            return string.Format("SELECT ((SELECT COALESCE(SUM(DYCXPM),0) FROM PHYESLD WHERE DYNRTA = '{0}') +  H3R6VA) AS MONTO FROM PHYESAT WHERE H3NRTA = '{0}'", Card);
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
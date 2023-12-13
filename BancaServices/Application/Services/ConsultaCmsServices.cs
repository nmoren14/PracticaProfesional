using BancaServices.Domain.Interfaces;
using IBM.Data.DB2;
using IBM.Data.DB2.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using System.Data;

namespace BancaServices.Application.Services
{
    public class ConsultaCmsServices : IConsultasCms
    {
        private readonly NLog.ILogger logger = LogManager.GetCurrentClassLogger();
        private readonly IConfiguration _configuration;

        public ConsultaCmsServices(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<JArray> ConsultaBinMastercard()
        {
            JArray result = new JArray();
            try
            {
                string ConString = _configuration["FACT"].ToString();
                string query = "select BICDBI, BIDSBN, TTCDTP from PHYBINS left join PHYTPPR on BICDBI = TTCDBI";

                using (DB2Connection connection = new DB2Connection(ConString))
                {
                    DB2Command command = new DB2Command();
                    JObject registro;
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
                                registro = new JObject();
                                if (!reader.IsDBNull(0))
                                {
                                    registro.Add(reader.GetName(0), reader.GetString(0));
                                }
                                else
                                {
                                    registro.Add(reader.GetName(0), " ");
                                }
                                if (!reader.IsDBNull(1))
                                {
                                    registro.Add(reader.GetName(1), reader.GetString(1).TrimEnd());
                                }
                                else
                                {
                                    registro.Add(reader.GetName(1), " ");
                                }
                                if (!reader.IsDBNull(2))
                                {
                                    registro.Add(reader.GetName(2), reader.GetString(2).TrimEnd());
                                }
                                else
                                {
                                    registro.Add(reader.GetName(2), " ");
                                }
                                result.Add(registro);
                            }
                        }
                        connection.Close();
                    }
                    catch (Exception ex)
                    {
                        connection.Close();
                        throw ex;
                    }
                }
            }
            catch (Exception ex)
            {
                result = new JArray();
                JObject error = new JObject();
                error.Add("Error", ex.ToString());
                result.Add(error);
                return null;
            }
            return result;
        }

        public async Task<JObject> ConsultaDiaCms()
        {
            JObject respuesta = new JObject();
            try
            {
                DateTime fechaActual = DateTime.Now;
                int ano = fechaActual.Year;
                int mes = fechaActual.Month;
                int dia = fechaActual.Day - 1;
                if (dia == 0)
                {
                    fechaActual = fechaActual.AddDays(-1);
                    dia = fechaActual.Day;
                }

                string ConString = _configuration["FACT"].ToString();
                string query = string.Format(@"SELECT BHI2AH FROM phyclau WHERE BHCDCC ='423' and BHV0KR={0} and BHV1KR={1} and BHV2KR={2}", ano, mes, dia);

                using (DB2Connection connection = new DB2Connection(ConString))
                {
                    DB2Command command = new DB2Command();
                    JObject registro;
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
                                registro = new JObject();
                                if (!reader.IsDBNull(0))
                                {
                                    if (reader.GetString(0) == "T")
                                    {
                                        respuesta = new JObject();
                                        respuesta.Add("Code", "00");
                                        respuesta.Add("Descripcion", "Exitoso");
                                        respuesta.Add("Estado", reader.GetString(0));
                                        break;
                                    }
                                    else
                                    {
                                        respuesta = new JObject();
                                        respuesta.Add("Code", "01");
                                        respuesta.Add("Descripcion", "Dia no terminado");
                                        respuesta.Add("Estado", reader.GetString(0));
                                    }
                                }
                            }
                        }
                        connection.Close();
                    }
                    catch (Exception ex)
                    {
                        respuesta = new JObject();
                        respuesta.Add("Code", "02");
                        respuesta.Add("Error", ex.ToString());
                        connection.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                respuesta = new JObject();
                respuesta.Add("Code", "02");
                respuesta.Add("Error", ex.ToString());
                return null;
            }
            return respuesta;
        }
        //Termina el ConsultaDiaCms

        public async Task<JArray> ConsultaBloqueoMora()
        {
            JArray result = new JArray();
            try
            {
                DateTime fecha = DateTime.Now;
                /* Se ejecutara los días 13 y 26, con los registros del cliente del 10 y 23 respectivamente. */
                fecha = fecha.AddDays(-3);
                string fechaActual = fecha.ToString("yyyyMMdd");

                string ConString = _configuration["FACT"].ToString();
                string query = string.Format(@"SELECT phynove.HCTPNB AS TARJETA, phynove.HCBKNB AS FECHA, PHYESAT.h3t1nb AS TELEFONO, PHYESAT.H3CDAB FROM phynove 
                                                INNER JOIN PHYESAT ON PHYNOVE.HCTPNB = PHYESAT.H3NRTA 
                                                WHERE phynove.HCFNCD='480' and PHYESAT.H3CDAB <> '0861'
                                                and SUBSTRING(phynove.HCTPNB, 1, 6) <> '899902' and SUBSTRING(phynove.HCTPNB, 1, 6) <> '899901' 
                                                and SUBSTRING(phynove.HCTPNB, 1, 6) <> '899900'
                                                and phynove.HCBKNB={0} and phynove.HCKXTX='B'", fechaActual);
                //String query = String.Format(@"SELECT phynove.HCTPNB AS TARJETA, phynove.HCBKNB AS FECHA, PHYESAT.h3t1nb AS TELEFONO FROM phynove 
                //                                INNER JOIN PHYESAT ON PHYNOVE.HCTPNB = PHYESAT.H3NRTA 
                //                                WHERE phynove.HCFNCD='480' and phynove.HCBKNB=20210323 and phynove.HCKXTX='B' LIMIT 1");

                using (DB2Connection connection = new DB2Connection(ConString))
                {
                    DB2Command command = new DB2Command();
                    JObject registro;
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
                                registro = new JObject();
                                if (!reader.IsDBNull(0))
                                {
                                    registro.Add(reader.GetName(0), reader.GetString(0));
                                }
                                else
                                {
                                    registro.Add(reader.GetName(0), " ");
                                }
                                if (!reader.IsDBNull(1))
                                {
                                    registro.Add(reader.GetName(1), reader.GetString(1));
                                }
                                else
                                {
                                    registro.Add(reader.GetName(1), " ");
                                }
                                if (!reader.IsDBNull(2))
                                {
                                    registro.Add(reader.GetName(2), reader.GetString(2));
                                }
                                else
                                {
                                    registro.Add(reader.GetName(2), " ");
                                }
                                result.Add(registro);
                            }
                        }
                        connection.Close();
                    }
                    catch (Exception ex)
                    {
                        connection.Close();
                        throw ex;
                    }
                }
            }
            catch (Exception ex)
            {
                result = new JArray();
                JObject error = new JObject();
                error.Add("Error", ex.ToString());
                result.Add(error);
                return null;
            }
            return result;

        }
        //Termina consulta de Mora

        public async Task<JArray> ConsultaBloqueoSobreCupo()
        {
            JArray result = new JArray();
            try
            {
                DateTime fecha = DateTime.Now;
                fecha = fecha.AddDays(-1);
                string fechaActual = fecha.ToString("yyyyMMdd");
                //String fechaActual = DateTime.Now.ToString("yyyyMMdd");
                string ConString = _configuration["FACT"].ToString();
                string query = string.Format(@"SELECT phynove.HCTPNB AS TARJETA, phynove.HCBKNB AS FECHA, PHYESAT.h3t1nb AS  TELEFONO, 
                                                (H3F3VA - ((SELECT COALESCE(SUM(DYCXPM),0) FROM PHYESLD WHERE DYNRTA = H3NRTA)+ H3R6VA)) AS SOBRECUPO, PHYESAT.H3CDAB
                                                FROM phynove 
                                                INNER JOIN PHYESAT ON PHYNOVE.HCTPNB = PHYESAT.H3NRTA
                                                WHERE phynove.HCFNCD='500' and phynove.HCBKNB={0} and phynove.HCKXTX ='B'
                                                and SUBSTRING(phynove.HCTPNB, 1, 6) <> '899902' and SUBSTRING(phynove.HCTPNB, 1, 6) <> '899901' 
                                                and SUBSTRING(phynove.HCTPNB, 1, 6) <> '899900' and PHYESAT.H3CDAB <> '0861'", fechaActual);
                //String query = String.Format(@"SELECT phynove.HCTPNB AS TARJETA, phynove.HCBKNB AS FECHA, PHYESAT.h3t1nb AS  TELEFONO, 
                //                                (H3F3VA - ((SELECT COALESCE(SUM(DYCXPM),0) FROM PHYESLD WHERE DYNRTA = H3NRTA)+ H3R6VA)) AS SOBRECUPO
                //                                FROM phynove 
                //                                INNER JOIN PHYESAT ON PHYNOVE.HCTPNB = PHYESAT.H3NRTA
                //                                WHERE phynove.HCFNCD='500' and phynove.HCBKNB=20210323 and phynove.HCKXTX ='B' LIMIT 1");

                using (DB2Connection connection = new DB2Connection(ConString))
                {
                    DB2Command command = new DB2Command();
                    JObject registro;
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
                                registro = new JObject();
                                if (!reader.IsDBNull(0))
                                {
                                    registro.Add(reader.GetName(0), reader.GetString(0));
                                }
                                else
                                {
                                    registro.Add(reader.GetName(0), " ");
                                }
                                if (!reader.IsDBNull(1))
                                {
                                    registro.Add(reader.GetName(1), reader.GetString(1));
                                }
                                else
                                {
                                    registro.Add(reader.GetName(1), " ");
                                }
                                if (!reader.IsDBNull(2))
                                {
                                    registro.Add(reader.GetName(2), reader.GetString(2));
                                }
                                else
                                {
                                    registro.Add(reader.GetName(2), " ");
                                }
                                if (!reader.IsDBNull(3))
                                {
                                    registro.Add(reader.GetName(3), reader.GetString(3));
                                }
                                else
                                {
                                    registro.Add(reader.GetName(3), " ");
                                }
                                result.Add(registro);
                            }
                        }
                        connection.Close();
                    }
                    catch (Exception ex)
                    {
                        connection.Close();
                        throw ex;
                    }
                }
            }
            catch (Exception ex)
            {
                result = new JArray();
                JObject error = new JObject();
                error.Add("Error", ex.ToString());
                result.Add(error);
                return null;
            }
            return result;
        }
        //Termina Consulta Sobrecupo

        public async Task<bool> ConsultaDiaFacturacion(string ciclo)
        {
            bool result = false;
            try
            {
                DateTime fechaActual = DateTime.Now;
                int ano = fechaActual.Year;
                int mes = fechaActual.Month;

                string ConString = _configuration["FACT"].ToString();
                string query = string.Format(@"SELECT DDHHTX from PHYCACI WHERE DDUKNB = '{0}' AND DDULNB = '{1}' AND DDFKCD = '{2}'", ano, mes, ciclo);

                using (DB2Connection connection = new DB2Connection(ConString))
                {
                    DB2Command command = new DB2Command();
                    JObject registro;
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
                                registro = new JObject();
                                if (!reader.IsDBNull(0))
                                {
                                    if (reader.GetString(0) == "5")
                                    {
                                        result = true;
                                        break;
                                    }
                                }
                            }
                        }
                        connection.Close();
                    }
                    catch (Exception ex)
                    {
                        connection.Close();
                        result = false;
                    }
                }
            }
            catch (Exception ex)
            {
                result = false;
            }
            return result;
        }
        //Termina Consulta Dia de Facturacion

        public async Task<JArray> ConsultaCancelacionTarjeta()
        {
            JArray result = new JArray();
            try
            {
                DateTime fecha = DateTime.Now;
                DateTime fechaDos = DateTime.Now;
                fecha = fecha.AddDays(-1);
                fechaDos = fecha.AddDays(-3);
                string fechaActual = fecha.ToString("yyyyMMdd");
                string fechaActualDos = fechaDos.ToString("yyyyMMdd");
                //String fechaActual = DateTime.Now.ToString("yyyyMMdd");
                string ConString = _configuration["FACT"].ToString();
                string query = string.Format(@"SELECT DISTINCT phynove.HCTPNB AS TARJETA, phynove.HCBKNB AS FECHA, PHYESAT.h3t1nb AS TELEFONO, phynove.HCQ8NB, phyrequ.K8MFCD, phynove.HCKVTX, PHYESAT.H3CDAB FROM phynove 
                                               INNER JOIN PHYESAT ON PHYNOVE.HCTPNB = PHYESAT.H3NRTA 
                                               INNER JOIN PHYREQU ON PHYNOVE.HCTPNB = PHYREQU.K8NRTA
                                               WHERE phynove.HCFNCD='100' and phynove.HCQ8NB = '03' and phyrequ.K8MFCD <> '8A' and phyrequ.K8GNST <> '01' and PHYESAT.H3CDAB <> '0861'
                                               and phynove.HCKVTX <> '09' and phynove.HCKVTX <> '12' and phynove.HCKVTX <> '14'
                                               and SUBSTRING(phynove.HCTPNB, 1, 6) <> '899902' and SUBSTRING(phynove.HCTPNB, 1, 6) <> '899901' 
                                               and SUBSTRING(phynove.HCTPNB, 1, 6) <> '899900'
                                               and phynove.HCBKNB={0} and phynove.HCKXTX!='D'", fechaActual);
                //String query = String.Format(@"SELECT phynove.HCTPNB AS TARJETA, phynove.HCBKNB AS FECHA, PHYESAT.h3t1nb AS TELEFONO FROM phynove 
                //                                INNER JOIN PHYESAT ON PHYNOVE.HCTPNB = PHYESAT.H3NRTA 
                //                                WHERE phynove.HCFNCD='480' and phynove.HCBKNB=20210323 and phynove.HCKXTX='B' LIMIT 1");

                using (DB2Connection connection = new DB2Connection(ConString))
                {
                    DB2Command command = new DB2Command();
                    JObject registro;
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
                                registro = new JObject();
                                if (!reader.IsDBNull(0))
                                {
                                    registro.Add(reader.GetName(0), reader.GetString(0));
                                }
                                else
                                {
                                    registro.Add(reader.GetName(0), " ");
                                }
                                if (!reader.IsDBNull(1))
                                {
                                    registro.Add(reader.GetName(1), reader.GetString(1));
                                }
                                else
                                {
                                    registro.Add(reader.GetName(1), " ");
                                }
                                if (!reader.IsDBNull(2))
                                {
                                    registro.Add(reader.GetName(2), reader.GetString(2));
                                }
                                else
                                {
                                    registro.Add(reader.GetName(2), " ");
                                }
                                result.Add(registro);
                            }
                        }
                        connection.Close();
                    }
                    catch (Exception ex)
                    {
                        connection.Close();
                        throw ex;
                    }
                }
            }
            catch (Exception ex)
            {
                result = new JArray();
                JObject error = new JObject();
                error.Add("Error", ex.ToString());
                result.Add(error);
                return null;
            }
            return result;

        }
        //Termina consulta de Cancelacion Tarjeta

        public async Task<JArray> ConsultaCastigosCartera()
        {
            JArray result = new JArray();
            try
            {
                DateTime fecha = DateTime.Now;
                /* Se ejecutara un día anteriores al último del mes */
                fecha = new DateTime(fecha.Year, fecha.Month + 1, 1).AddDays(-2);
                string fechaActual = fecha.ToString("yyyyMMdd");
                //String fechaActual = DateTime.Now.ToString("yyyyMMdd");
                string ConString = _configuration["FACT"].ToString();
                string query = string.Format(@"SELECT phynove.HCTPNB AS TARJETA, phynove.HCBKNB AS FECHA, PHYESAT.h3t1nb AS TELEFONO, PHYESAT.H3CDAB FROM phynove 
                                                INNER JOIN PHYESAT ON PHYNOVE.HCTPNB = PHYESAT.H3NRTA 
                                                WHERE phynove.HCFNCD='410' and phynove.HCBKNB={0} and phynove.HCKXTX LIKE '%B%'
                                                and SUBSTRING(phynove.HCTPNB, 1, 6) <> '899902' and SUBSTRING(phynove.HCTPNB, 1, 6) <> '899901' 
                                                and SUBSTRING(phynove.HCTPNB, 1, 6) <> '899900' and PHYESAT.H3CDAB <> '0861'", fechaActual);
                //String query = String.Format(@"SELECT phynove.HCTPNB AS TARJETA, phynove.HCBKNB AS FECHA, PHYESAT.h3t1nb AS TELEFONO FROM phynove 
                //                                INNER JOIN PHYESAT ON PHYNOVE.HCTPNB = PHYESAT.H3NRTA 
                //                                WHERE phynove.HCFNCD='480' and phynove.HCBKNB=20210323 and phynove.HCKXTX='B' LIMIT 1");

                using (DB2Connection connection = new DB2Connection(ConString))
                {
                    DB2Command command = new DB2Command();
                    JObject registro;
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
                                registro = new JObject();
                                if (!reader.IsDBNull(0))
                                {
                                    registro.Add(reader.GetName(0), reader.GetString(0));
                                }
                                else
                                {
                                    registro.Add(reader.GetName(0), " ");
                                }
                                if (!reader.IsDBNull(1))
                                {
                                    registro.Add(reader.GetName(1), reader.GetString(1));
                                }
                                else
                                {
                                    registro.Add(reader.GetName(1), " ");
                                }
                                if (!reader.IsDBNull(2))
                                {
                                    registro.Add(reader.GetName(2), reader.GetString(2));
                                }
                                else
                                {
                                    registro.Add(reader.GetName(2), " ");
                                }
                                result.Add(registro);
                            }
                        }
                        connection.Close();
                    }
                    catch (Exception ex)
                    {
                        connection.Close();
                        throw ex;
                    }
                }
            }
            catch (Exception ex)
            {
                result = new JArray();
                JObject error = new JObject();
                error.Add("Error", ex.ToString());
                result.Add(error);
                return null;
            }
            return result;

        }
        //Termina consulta de Castigos de Cartera

        public async Task<JArray> ConsultaArregloCartera()
        {
            JArray result = new JArray();
            try
            {
                DateTime fecha = DateTime.Now;
                fecha = fecha.AddDays(-1);
                string fechaActual = fecha.ToString("yyyyMMdd");
                //String fechaActual = DateTime.Now.ToString("yyyyMMdd");
                string ConString = _configuration["FACT"].ToString();
                string query = string.Format(@"SELECT phynove.HCTPNB AS TARJETA, phynove.HCBKNB AS FECHA, PHYESAT.h3t1nb AS TELEFONO, PHYESAT.H3CDAB FROM phynove 
                                                INNER JOIN PHYESAT ON PHYNOVE.HCTPNB = PHYESAT.H3NRTA 
                                                WHERE phynove.HCFNCD='140' and phynove.HCBKNB={0} and phynove.HCKXTX='B'
                                                and SUBSTRING(phynove.HCTPNB, 1, 6) <> '899902' and SUBSTRING(phynove.HCTPNB, 1, 6) <> '899901' 
                                                and SUBSTRING(phynove.HCTPNB, 1, 6) <> '899900' and PHYESAT.H3CDAB <> '0861'", fechaActual);
                //String query = String.Format(@"SELECT phynove.HCTPNB AS TARJETA, phynove.HCBKNB AS FECHA, PHYESAT.h3t1nb AS TELEFONO FROM phynove 
                //                                INNER JOIN PHYESAT ON PHYNOVE.HCTPNB = PHYESAT.H3NRTA 
                //                                WHERE phynove.HCFNCD='480' and phynove.HCBKNB=20210323 and phynove.HCKXTX='B' LIMIT 1");

                using (DB2Connection connection = new DB2Connection(ConString))
                {
                    DB2Command command = new DB2Command();
                    JObject registro;
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
                                registro = new JObject();
                                if (!reader.IsDBNull(0))
                                {
                                    registro.Add(reader.GetName(0), reader.GetString(0));
                                }
                                else
                                {
                                    registro.Add(reader.GetName(0), " ");
                                }
                                if (!reader.IsDBNull(1))
                                {
                                    registro.Add(reader.GetName(1), reader.GetString(1));
                                }
                                else
                                {
                                    registro.Add(reader.GetName(1), " ");
                                }
                                if (!reader.IsDBNull(2))
                                {
                                    registro.Add(reader.GetName(2), reader.GetString(2));
                                }
                                else
                                {
                                    registro.Add(reader.GetName(2), " ");
                                }
                                result.Add(registro);
                            }
                        }
                        connection.Close();
                    }
                    catch (Exception ex)
                    {
                        connection.Close();
                        throw ex;
                    }
                }
            }
            catch (Exception ex)
            {
                result = new JArray();
                JObject error = new JObject();
                error.Add("Error", ex.ToString());
                result.Add(error);
                return null;
            }
            return result;

        }
        //Termina consulta de Arreglo Cartera

        public async Task<List<(string, string, string)>> ConsultaActivacionTarjeta()
        {
            List<(string, string, string)> result = new List<(string, string, string)>();
            try
            {
                DateTime fecha = DateTime.Now;
                //fecha = fecha.AddDays(-5);
                string fechaActual = fecha.ToString("yyyyMMdd");
                //String fechaActual = DateTime.Now.ToString("yyyyMMdd");
                string ConString = _configuration["FACT"].ToString();
                string query = string.Format(@"SELECT phynove.HCTPNB AS TARJETA, phynove.HCBKNB AS FECHA , PHYESAT.h3t1nb AS TELEFONO 
                                                FROM phynove 
                                                INNER JOIN PHYESAT ON PHYNOVE.HCTPNB = PHYESAT.H3NRTA
                                                WHERE HCFNCD='510' and HCKXTX='D' and HCL6TX <> 'SISTEMA' and phynove.HCBKNB={0} and SUBSTRING(phynove.HCTPNB, 1, 6) 
                                                IN (SELECT CBBIN FROM phsbin WHERE cbcro='CRO')", fechaActual);

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
                            string Tarjeta = string.Empty, Telefono = string.Empty, Fecha = string.Empty;
                            if (!reader.IsDBNull(0))
                            {
                                if (!reader.IsDBNull(0))
                                {
                                    Tarjeta = reader.GetValue(0).ToString().Trim();
                                }
                                if (!reader.IsDBNull(1))
                                {
                                    Fecha = reader.GetValue(1).ToString();
                                }
                                if (!reader.IsDBNull(2))
                                {
                                    Telefono = reader.GetValue(2).ToString();
                                }
                                else
                                {
                                    continue;
                                }
                                result.Add((Tarjeta, Fecha, Telefono));
                            }
                        }
                        connection.Close();
                    }
                    catch (Exception ex)
                    {
                        connection.Close();
                        throw ex;
                    }
                }
            }
            catch (Exception ex)
            {
                result = new List<(string, string, string)>();
            }
            return result;

        }
        //Termina consulta de Activacion Tarjeta

        public async Task<JArray> ConsultaActivacionTarjetaTipos()
        {
            JArray result = new JArray();
            try
            {
                //DateTime fecha = DateTime.Now;
                //fecha = fecha.AddDays(-1);
                //String fechaActual = fecha.ToString("yyyyMMdd");
                //String fechaActual = DateTime.Now.ToString("yyyyMMdd");
                string ConString = _configuration["FACT"].ToString();
                string query = string.Format(@"SELECT phsbin.CBBIN AS INICIOTC, phsbin.CBCRO AS TIPOTC, phsbin.CBDES AS DESCTC FROM phsbin WHERE cbcro='CRO'");

                using (DB2Connection connection = new DB2Connection(ConString))
                {
                    DB2Command command = new DB2Command();
                    JObject registro;
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
                                registro = new JObject();
                                if (!reader.IsDBNull(0))
                                {
                                    registro.Add(reader.GetName(0), reader.GetString(0));
                                }
                                else
                                {
                                    registro.Add(reader.GetName(0), " ");
                                }
                                if (!reader.IsDBNull(1))
                                {
                                    registro.Add(reader.GetName(1), reader.GetString(1));
                                }
                                else
                                {
                                    registro.Add(reader.GetName(1), " ");
                                }
                                if (!reader.IsDBNull(2))
                                {
                                    registro.Add(reader.GetName(2), reader.GetString(2));
                                }
                                else
                                {
                                    registro.Add(reader.GetName(2), " ");
                                }
                                result.Add(registro);
                            }
                        }
                        connection.Close();
                    }
                    catch (Exception ex)
                    {
                        connection.Close();
                        throw ex;
                    }
                }
            }
            catch (Exception ex)
            {
                result = new JArray();
                JObject error = new JObject();
                error.Add("Error", ex.ToString());
                result.Add(error);
                return null;
            }
            return result;

        }
        //Termina consulta de Activacion Tarjeta Tipos de Tarjetas

        public async Task<JArray> ConsultaInactivarCampanas(string cedula)
        {
            JArray result = new JArray();
            try
            {
                DateTime fecha = DateTime.Now;
                fecha = fecha.AddDays(-1);
                string fechaActual = fecha.ToString("yyyyMMdd");
                //String fechaActual = DateTime.Now.ToString("yyyyMMdd");
                string ConString = _configuration["FACT"].ToString();
                string query = string.Format(@"SELECT * FROM phymoap, phyesat
                                                WHERE h3nrta=d7nrta AND d7cdtt='04'
                                                AND h3unnb={0}
                                                AND d7fmap={1}", cedula, fechaActual);

                using (DB2Connection connection = new DB2Connection(ConString))
                {
                    DB2Command command = new DB2Command();
                    JObject registro;
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
                                registro = new JObject();
                                if (!reader.IsDBNull(0))
                                {
                                    registro.Add(reader.GetName(0), reader.GetString(0));
                                }
                                else
                                {
                                    registro.Add(reader.GetName(0), " ");
                                }
                                if (!reader.IsDBNull(1))
                                {
                                    registro.Add(reader.GetName(1), reader.GetString(1));
                                }
                                else
                                {
                                    registro.Add(reader.GetName(1), " ");
                                }
                                if (!reader.IsDBNull(64))
                                {
                                    registro.Add(reader.GetName(64), reader.GetString(64));
                                }
                                else
                                {
                                    registro.Add(reader.GetName(64), " ");
                                }
                                result.Add(registro);
                            }
                        }
                        connection.Close();
                    }
                    catch (Exception ex)
                    {
                        connection.Close();
                        throw ex;
                    }
                }
            }
            catch (Exception ex)
            {
                result = new JArray();
                JObject error = new JObject();
                error.Add("Error", ex.ToString());
                result.Add(error);
                return null;
            }
            return result;

        }
        //Termina consulta de Compra de Productos - Inactivar Campanas

        public async Task<JObject> ConsultaAmparadas(string Tarjeta)
        {
            JObject result = new JObject();
            JArray Tarjetas = new JArray();
            try
            {
                string ConString = _configuration["FACT"].ToString();
                string query = string.Format("SELECT H3NRTA, H3UNNB, H3PZNB, H3UENB, H3CDTP, H3CDTI , H3MQNB, H3R3VA, TTSTP1 FROM PHYESAT, PHYTPPR WHERE H3TZNB = '{0}' AND H3CDTP = TTCDTP", Tarjeta);

                using (DB2Connection connection = new DB2Connection(ConString))
                {
                    DB2Command command = new DB2Command();
                    JObject row;
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
                                row = new JObject();
                                row.Add("Amparada", reader.GetValue(0).ToString());
                                row.Add("Identificacion", reader.GetValue(1).ToString().TrimEnd());
                                row.Add("Nombre", reader.GetValue(2).ToString().TrimEnd());
                                row.Add("Estado", JArray.FromObject(SerfiUtils.Utils.ConsultarEstadoProductoVector(reader.GetValue(3).ToString())));
                                row.Add("Producto", reader.GetValue(4).ToString().TrimEnd());
                                row.Add("TipoIdentificacion", reader.GetValue(5).ToString().TrimEnd());
                                row.Add("FechaEmision", reader.GetValue(6).ToString().TrimEnd());
                                row.Add("Saldo", reader.GetValue(7).ToString().TrimEnd());
                                row.Add("TipoProducto", reader.GetValue(8).ToString().TrimEnd());
                                Tarjetas.Add(row);
                            }
                        }
                        connection.Close();
                    }
                    catch (Exception ex)
                    {
                        connection.Close();
                        throw ex;
                    }
                }

                if (Tarjetas.Count > 0)
                {
                    result.Add("Codigo", "00");
                    result.Add("Descripcion", "Exitoso");
                    result.Add("Amparadas", Tarjetas);
                }
                else
                {
                    result.Add("Codigo", "01");
                    result.Add("Descripcion", "No se han encontrado tarjetas amparadas para esta tarjeta");
                    result.Add("Amparadas", null);
                }
            }
            catch (Exception ex)
            {
                result = new JObject();
                result.Add("Codigo", "02");
                result.Add("Descripcion", "Ha ocurrido un Error " + ex.Message);
                result.Add("Amparadas", null);
            }
            return result;
        }
        public async Task<string> ObtenerTarjetaPorNumRotativo(string numeroRotativo)
        {
            string tarjeta = string.Empty;
            string connString = _configuration.GetConnectionString("FACT");
            try
            {
                using (DB2Connection conn = new DB2Connection(connString))
                {
                    using (DB2Command command = new DB2Command())
                    {
                        command.Connection = conn;
                        command.CommandText = @"SELECT NTRNTNR FROM phsrcrtnr WHERE NTRRTNR = @rotativo ";
                        command.Parameters.Add("@rotativo", numeroRotativo);
                        conn.Open();
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (reader.Read())
                            {
                                tarjeta = reader.GetString(0);
                            }
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error ObtenerTarjetaPorNumRotativo {ex.ToString()}");
            }

            return tarjeta.Trim();
        }

        public async Task<string> ObtenerNumRotativoPorDocumento(string idCliente, string tipoDocumento)
        {
            string NumRotativo = string.Empty;
            try
            {
                string connString = _configuration.GetConnectionString("FACT");
                using (DB2Connection conn = new DB2Connection(connString))
                {
                    using (DB2Command command = new DB2Command())
                    {
                        command.Connection = conn;
                        command.CommandText = @"SELECT T.H3NRTA FROM PHYESAT T WHERE T.H3CDTP IN('CR', 'CR2', 'CR3')
                                            AND SUBSTR(LPAD(T.H3UENB, 10, '0'), 10, 1) = 0
                                            AND SUBSTR(LPAD(T.H3UENB, 10, '0'), 1, 1) = 0
                                            AND T.H3PZNB <> 'PRE-ISSUED'
                                            AND T.H3FKCD <> 'HM' AND T.H3UNNB = @idCliente AND T.H3CDTI = @tipoDoc";
                        command.Parameters.Add("@idCliente", idCliente);
                        command.Parameters.Add("@tipoDoc", tipoDocumento);
                        conn.Open();
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (reader.Read())
                            {
                                NumRotativo = reader.GetString(0);
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                logger.Error($"Error ObtenerNumRotativoPorDocumento {ex.ToString()}");
            }
            return NumRotativo.Trim();
        }

        public async Task<JObject> QueryCMS(JObject d)
        {
            JObject res = new JObject();
            string connectionString = d["Conexion"].ToString();
            string sql = d["Sql"].ToString();


            using (DB2Connection con = new DB2Connection(connectionString))
            {

                await con.OpenAsync();
                DB2Command cmd = new DB2Command(sql, con)
                {
                    CommandType = CommandType.Text
                };
                switch (d["Op"].ToString())
                {
                    #region Select
                    case "01":
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (reader.HasRows)
                            {
                                DataTable dt = new DataTable();
                                dt.Load(reader);
                                JArray array = JArray.Parse(JsonConvert.SerializeObject(dt));
                                res["Codigo"] = "01";
                                res["Descripcion"] = "Datos Encontrados";
                                res["Data"] = array;
                            }
                            else
                            {
                                res["Codigo"] = "03";
                                res["Descripcion"] = "No hay datos";
                            }

                        }



                        break;
                    #endregion
                    #region Insert update Delete
                    case "02":
                        await cmd.ExecuteNonQueryAsync();
                        res["Codigo"] = "01";
                        res["Descripcion"] = "Ejecutado";
                        break;
                        #endregion

                }

                ///

                return res;

            }
        }


        public async Task<bool> EsTarjetaEmpresarialPadre(string NumeroTarjeta)
        {
            bool Reps = false;

            try
            {
                JObject req = new JObject
                {
                    ["Conexion"] = _configuration.GetConnectionString("FACT"),
                    ["Op"] = "01",
                    ["Sql"] = $"select 1 as Existe from PHYEMRR where AJNRTA='{NumeroTarjeta}'"
                };

                JObject re = await QueryCMS(req);
                if (re["Codigo"].ToString().Equals("01"))
                {
                    Reps = re["Data"][0]["Existe".ToUpper()].ToString().Trim().Equals("1");
                }

            }
            catch (Exception ex)
            {
                logger.Error<Exception>("Error EsTarjetaEmpresarialPadre Error={Error}", ex);
            }

            return Reps;

        }

        public async Task<string> ObtenerReferenciaPorTarjetaoPorNumRotativo(string NumTarjetaORotativo)
        {
            string Referencia = string.Empty;
            string connString = _configuration.GetConnectionString("FACT");
            try
            {
                using (DB2Connection conn = new DB2Connection(connString))
                {
                    using (DB2Command command = new DB2Command())
                    {
                        command.Connection = conn;
                        command.CommandText = @"SELECT C1CTAN FROM PHSCTAA WHERE C1CTA = @tarjetaorotativo ";
                        command.Parameters.Add("@tarjetaorotativo", NumTarjetaORotativo);
                        conn.Open();
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (reader.Read())
                            {
                                Referencia = reader.GetString(0);
                            }
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error ObtenerReferenciaPorTarjetaoPorNumRotativo {ex.ToString()}");
            }

            return Referencia;
        }

        public async Task<string> ObtenerTipoProductoPorNumRotativoOTC(string NumTarjetaORotativo)
        {
            string TipoProducto = string.Empty;
            try
            {
                string connString = _configuration.GetConnectionString("FACT");
                using (DB2Connection conn = new DB2Connection(connString))
                {
                    using (DB2Command command = new DB2Command())
                    {
                        command.Connection = conn;
                        command.CommandText = @"SELECT T.H3CDTP FROM PHYESAT T WHERE T.H3CDTP IN('CR', 'CR2', 'CR3') AND T.H3NRTA=@NumTarjetaORotativo";
                        command.Parameters.Add("@NumTarjetaORotativo", NumTarjetaORotativo);
                        conn.Open();
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (reader.Read())
                            {
                                TipoProducto = reader.GetString(0);
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                logger.Error($"Error ObtenerTipoProductoPorNumRotativoOTC {ex.ToString()}");
            }
            return TipoProducto.Trim();
        }
    }
}
using BancaServices.Contexto;
using Newtonsoft.Json.Linq;
using System.Data.Entity;
using System.Net;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using System.Data;
using System.Text;

namespace BancaServices.Application.Services.SerfiUtils
{
    public class Utils
    {

        public enum Sistema { EIBS, AUTO };
        public const string RESUMEN = "Resmen";
        public const string DETALLE = "Detalle";

        public enum TiposTransferencias
        {
            Internas = 1,
            OtrosBancos = 2
        }

        public static string HomologarRespuestaACH(string codigoEibs)
        {
            /*
            Codigos que vienen de ibs
            0  - OK
            17 - EXCEDE MONTO DIARIO
            18 - EXCEDE MONTO MENSUAL
            23 - CLIENTE NO EXISTE
            24 - ID NO CLIENTE REGISTRADO
            25 - CLIENTE INACTIVO
            26 - TF OPER. RESTRINGIDA
            27 - DP OPER. RESTRINGIDA
            28 - RT OPER. RESTRINGIDA
            50 - TX NO EXISTE
            51 - TID - NO EXISTE
            99 - EL MONTO DIA NO PUEDE SUPERAR EL MONTO MAXIMO DE LA ENTIDA
             */


            int codIbs = 0;
            try
            {
                codIbs = int.Parse(codigoEibs);
            }
            catch (Exception)
            {
                return "9";

            }

            string codigoHomologado = string.Empty;
            if (codIbs >= 7 && codIbs <= 22 || codIbs == 29 || codIbs == 30)
            {//exede limite transaccional
                codigoHomologado = "8";
            }
            else if (codIbs == 166)
            {
                codigoHomologado = "2";
            }
            else if (codIbs == 164 || codIbs == 7662)
            {
                codigoHomologado = "3";
            }
            else if (codIbs == 41)
            {
                codigoHomologado = "4";
            }
            else if (codIbs == 163)
            {
                codigoHomologado = "7";
            }
            else if (codIbs == 862)
            {
                codigoHomologado = "11";
            }
            else
            {
                codigoHomologado = "9";
            }

            return codigoHomologado;

        }

        public static string HomologarTipoId(string tipoId, Sistema sistema)
        {
            string tipoIdHomologado = string.Empty;
            switch (tipoId)
            {
                case "1":
                case "01":
                    tipoIdHomologado = sistema.Equals(Sistema.EIBS) ? "CC" : "01";
                    break;
                case "2":
                case "02":
                    tipoIdHomologado = sistema.Equals(Sistema.EIBS) ? "CE" : "03";
                    break;
                case "3":
                case "03":
                    tipoIdHomologado = sistema.Equals(Sistema.EIBS) ? "NIT" : "02";
                    break;
                case "4":
                case "04":
                    tipoIdHomologado = sistema.Equals(Sistema.EIBS) ? "TI" : "04";
                    break;
                case "5":
                case "05":
                    tipoIdHomologado = sistema.Equals(Sistema.EIBS) ? "PAS" : "05";
                    break;
                case "9":
                case "09":
                    tipoIdHomologado = sistema.Equals(Sistema.EIBS) ? "RCN" : "09";
                    break;
            }

            return tipoIdHomologado;
        }


        public static string HomologarTipoIdCMS(string tipoId)
        {
            string tipoIdHomologado = string.Empty;
            switch (tipoId)
            {
                case "CC":
                    tipoIdHomologado = "1";
                    break;
                case "CE":
                    tipoIdHomologado = "2";
                    break;
                case "NIT":
                    tipoIdHomologado = "3";
                    break;
                case "TI":
                    tipoIdHomologado = "4";
                    break;
                case "PAS":
                    tipoIdHomologado = "5";
                    break;
                case "RCN":
                    tipoIdHomologado = "9";
                    break;
            }

            return tipoIdHomologado;
        }

        public static string LimpiarSaldos(string strSaldo)
        {
            var saldo = string.Empty;
            strSaldo = strSaldo.Replace("$", string.Empty).Replace(".", string.Empty).Trim();
            strSaldo = strSaldo.Replace(" ", "");
            strSaldo = strSaldo.Replace(',', '.');
            saldo = strSaldo;
            return saldo;
        }


        /// <summary>
        /// Permite consumir un servicio POST que devuelva un JObject
        /// </summary>
        /// <param name="url"></param>
        /// <param name="datosEntrada"></param>
        /// <param name="autorizacion">jobject que contiene campos  para basic Authorization
        /// Usuario tipo string
        /// Password tipo string
        /// </param>
        /// <returns>devuelve salida en Jobject</returns>
        public static async Task<JObject> ConsumirApiSalidaJObject(string url, JObject datosEntrada, JObject autorizacion = null)
        {

            JObject result = new JObject();
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";

            if (autorizacion != null)
            {
                string encoded = Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1").GetBytes(autorizacion["Usuario"].ToString() + ":" + autorizacion["Password"].ToString()));
                httpWebRequest.Headers.Add("Authorization", "Basic " + encoded);
            }


            using (var streamWriter = new StreamWriter(await httpWebRequest.GetRequestStreamAsync()))
            {
                streamWriter.Write(datosEntrada.ToString());
                streamWriter.Flush();
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

            throw new NotImplementedException();
        }

        /// <summary>
        /// permite ocultar los valores
        /// ejemplo de consumo OcultarValor("3293763", 4, '*', 10, true) da como resultado ******3763
        /// ejemplo de recote de valor OcultarValor("3293763", 4, '*', 4, true) da como resultado 3763 no lo rellena solo muestra lo que le pidas
        /// </summary>
        public static string OcultarValor(string texto, int NroCaractMostrar, char relleno, int longitudFinal, bool IsLeftOrRigth = true)
        {
            try
            {
                string final = string.Empty;
                int longitud = texto.Length - NroCaractMostrar;
                string ObtenerCadena = texto.Substring(longitud, NroCaractMostrar);
                if (IsLeftOrRigth)
                    final = ObtenerCadena.PadLeft(longitudFinal, relleno);
                else
                    final = ObtenerCadena.PadRight(longitudFinal, relleno);
                return final;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static string FormatoSaldo(string saldo)
        {
            string strSaldo = saldo.Replace("$", string.Empty).Replace(".", string.Empty).Replace(" ", string.Empty).Trim();
            strSaldo = strSaldo.Replace(',', '.');
            return strSaldo;
        }

        /// <summary>
        /// Permite validar correo electronico
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public static bool IsValidEmail(string email)
        {
            const string pattern = @"^([a-zA-Z0-9_\.\-])+\@(([a-zA-Z0-9\-])+\.)+([a-zA-Z0-9]{2,4})+$";

            var regex = new Regex(pattern, RegexOptions.IgnoreCase);

            return regex.IsMatch(email);
        }

        public static string RemoverTilde(string word)
        {

            string result = string.Empty;
            try
            {
                result = Regex.Replace(word.Normalize(NormalizationForm.FormD), @"[^a-zA-z0-9 ]+", "");
            }
            catch (Exception e)
            {

                Console.WriteLine(e.Message);
            }

            return result;
        }

        public static List<string> ConsultarEstadoProductoVector(string estado)
        {
            List<string> EstadosTarjeta = new List<string>();
            try
            {
                List<(string, int, int)> Estados = new List<(string, int, int)>();
                //Estados.Add(("NORMAL", 0, 0));//true
                Estados.Add(("MAL MANEJO", 1, 1));
                Estados.Add(("FALLECIMIENTO", 1, 2));
                Estados.Add(("DEVOLUCION VOLUNTARIA", 1, 3));
                Estados.Add(("IDENTIFICACION INVALIDA", 1, 4));
                Estados.Add(("NO RENOVACION", 1, 5));
                Estados.Add(("BLQ. FRAUDE", 1, 6));
                Estados.Add(("TARJETA GEMELA", 1, 7));
                Estados.Add(("EXTRAVIO REPORTADO", 1, 8));
                Estados.Add(("EXTRAVIO O ROBO", 1, 9));
                Estados.Add(("CASTIGO DE CARTERA", 2, 4));
                Estados.Add(("DUDOSO RECAUDO Y COBRO JUDICIAL", 2, 3));
                Estados.Add(("DUDOSO RECAUDO", 2, 2));
                Estados.Add(("COBRO PREJUDICIAL", 2, 1));
                Estados.Add(("DUDOSO RECAUDO PROXIMA FACTURACION", 3, 2));
                Estados.Add(("REESTRUCTURADO", 3, 1));
                Estados.Add(("CLAUSULA ACELERATORIA APLICADA", 4, 1));
                Estados.Add(("MORA", 5, 1));
                Estados.Add(("SOBRECUPO", 6, 1));
                Estados.Add(("BLOQUEO POR EXTENSION DE LA PPAL", 7, 1));
                Estados.Add(("BLOQUEO OFICINA", 8, 1));//true
                Estados.Add(("BLOQUEO AL DESPACHO", 9, 1));//true
                Estados.Add(("BLOQUEO POR INACTIVIDAD TARJETA", 10, 1));
                Estados.Add(("BLOQUEO TARJETA X CAMBIO NUM. TARJ.", 10, 2));
                Estados.Add(("SOBRECUPO GENERADO POR EXTRACUPO", 6, 2));


                if (!string.IsNullOrEmpty(estado))
                {
                    if (estado == "0")
                    {
                        EstadosTarjeta.Add("NORMAL");
                    }
                    else
                    {
                        estado = estado.PadLeft(11, '0');
                        var arrayEstado = estado.ToArray();

                        for (int i = 0; i < arrayEstado.Length; i++)
                        {
                            int valorPos = Convert.ToInt32(arrayEstado[i].ToString());
                            var (a, b, c) = Estados.Where(x => x.Item2 == i && x.Item3 == valorPos).FirstOrDefault();
                            if (!string.IsNullOrEmpty(a))
                            {
                                EstadosTarjeta.Add(a);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }
            return EstadosTarjeta;
        }

        public static async Task<string> HomologarCodBancoCMSToIBS(string CodigoBancoCms, NLog.ILogger logger)
        {

            string CodigoBancIBS = string.Empty;

            try
            {
                using (IntegradorContext context = new IntegradorContext())
                {
                    var Entidad = await context.entidades.Where(a => a.codigoCms.Equals(CodigoBancoCms)).FirstOrDefaultAsync();

                    if (Entidad != null)
                    {
                        CodigoBancIBS = Entidad.codigo;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Info($"Error HomologarCodBancoCMSToIBS {CodigoBancoCms} {ex.ToString()}");
            }

            return CodigoBancIBS;
        }
    }

    public class CustomDataTableConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(DataTable);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            DataTable table = (DataTable)value;
            JArray array = new JArray();
            foreach (DataRow row in table.Rows)
            {
                JObject obj = new JObject();
                foreach (DataColumn col in table.Columns)
                {
                    object val = row[col];
                    obj.Add(col.ColumnName, val != null ? val.ToString() : string.Empty);
                }
                array.Add(obj);
            }
            array.WriteTo(writer);
        }

        public override bool CanRead
        {
            get { return false; }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

    }

}
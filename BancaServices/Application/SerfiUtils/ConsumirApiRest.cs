using Newtonsoft.Json.Linq;
using System.Net;

namespace BancaServices.Application.Services.SerfiUtils
{
    public static class ConsumirApiRest
    {

        public const string TIPO_AUTORIZACION_BASIC = "Basic";
        public const string TIPO_AUTORIZACION_BEARER = "Bearer";
        public const string TIPO_HEADER_CUSTOM = "CUSTOM";
        public const string CONTENT_TYPE_JSON = "application/json";
        public const string CONTENT_TYPE_XFORM = "application/x-www-form-urlencoded";
        public const string METODO_POST = "POST";
        public const string METODO_GET = "GET";

        /// <summary>
        /// Permite consumir un servicio POST que devuelva un JObject
        /// </summary>
        /// <param name="url"></param>
        /// <param name="datosEntrada"></param>
        /// <param name="Metodo"></param>
        /// <param name="ContentType"></param>
        /// <param name="Headers"></param>
        /// <param name="Timeout"></param>
        /// <param name="logger"></param>
        /// <returns>JObject</returns>
        public static async Task<JObject> ConsumirApiSalidaJObject(string url, JObject datosEntrada, string Metodo = "POST", string ContentType = "application/json", JObject Headers = null, int Timeout = 2000, NLog.ILogger logger = null)
        {
            JObject result = new JObject();

            try
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                httpWebRequest.ContentType = ContentType;
                httpWebRequest.Accept = CONTENT_TYPE_JSON;
                httpWebRequest.Method = Metodo;
                httpWebRequest = SetHeader(httpWebRequest, Headers);
                httpWebRequest.Timeout = Timeout;
                httpWebRequest.ReadWriteTimeout = Timeout;



                if (!Metodo.Equals(METODO_GET))
                {
                    using (var streamWriter = new StreamWriter(await httpWebRequest.GetRequestStreamAsync()))
                    {
                        streamWriter.Write(datosEntrada.ToString());
                        streamWriter.Flush();
                        streamWriter.Close();
                    }

                }

                using (var httpResponse = (HttpWebResponse)await httpWebRequest.GetResponseAsync())
                {
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                    {

                        if (httpResponse.StatusDescription == "OK" || httpResponse.StatusCode.ToString() == "OK")
                        {
                            try
                            {
                                result = JObject.Parse(await streamReader.ReadToEndAsync());
                            }
                            catch (Exception ex)
                            {
                                result = new JObject();
                            }

                        }
                        else if (httpResponse.ContentType.Equals(CONTENT_TYPE_JSON))
                        {
                            try
                            {
                                result = JObject.Parse(await streamReader.ReadToEndAsync());
                            }
                            catch (Exception)
                            {
                                result = new JObject();
                            }

                        }
                        if (result.ToString().Contains("{},"))
                        {
                            string arreglaIBS = result.ToString().Replace("{},", "\"\",");
                            result = JObject.Parse(arreglaIBS);
                        }

                    }
                }
            }
            catch (WebException ex)
            {
                if (logger != null)
                {
                    logger.Error("Error Consumiendo: {URL} Error: {ERROR}", url, ex.ToString());

                }
            }
            catch (Exception ex)
            {
                if (logger != null)
                {
                    logger.Error("Error Consumiendo: {URL} Error: {ERROR}", url, ex.ToString());

                }

                result = new JObject();
            }



            return result;
        }

        /// <summary>
        /// Permite consumir un servicio POST que devuelva un JObject
        /// </summary>
        /// <param name="url"></param>
        /// <param name="datosEntrada"></param>
        /// <param name="Timeout"></param>
        /// <returns>devuelve salida en Jobject</returns>
        public static async Task<JArray> ConsumirApiSalidaJArray(string url, JObject datosEntrada, int Timeout = 2000)
        {
            JArray result = new JArray();
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.ContentType = CONTENT_TYPE_JSON;
            httpWebRequest.Accept = CONTENT_TYPE_JSON;
            httpWebRequest.Method = "POST";
            httpWebRequest.Timeout = Timeout;

            using (var streamWriter = new StreamWriter(await httpWebRequest.GetRequestStreamAsync()))
            {
                streamWriter.Write(datosEntrada.ToString());
                streamWriter.Flush();
                streamWriter.Close();
            }
            using (var httpResponse = (HttpWebResponse)await httpWebRequest.GetResponseAsync())
            {
                if (httpResponse.StatusDescription == "OK")
                {

                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                    {

                        var resu = await streamReader.ReadToEndAsync();

                        try
                        {

                            result = JArray.Parse(resu);

                        }
                        catch (Exception)//si el servicio responde con un jobject en ves de array
                        {
                            JObject objeto = new JObject();
                            result = new JArray();
                            objeto = JObject.Parse(resu);
                            result.Add(objeto);
                            return result;
                        }


                    }
                }
            }
            return result;
        }

        public static HttpWebRequest SetHeader(HttpWebRequest httpWebRequest, JObject Headers)
        {

            if (Headers != null)
            {
                string autori = "";

                if (Headers["Tipo"] != null)
                {
                    string tipo = Headers["Tipo"].ToString();
                    switch (tipo)
                    {
                        case TIPO_AUTORIZACION_BASIC:
                            string encoded = Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(Headers["Usuario"].ToString() + ":" + Headers["Password"].ToString()));
                            autori = $"{TIPO_AUTORIZACION_BASIC} {encoded}";
                            httpWebRequest.Headers.Add("Authorization", autori);
                            break;

                        case TIPO_AUTORIZACION_BEARER:
                            autori = $"{TIPO_AUTORIZACION_BEARER} {Headers["Token"].ToString()}";
                            httpWebRequest.Headers.Add("Authorization", autori);
                            break;

                        case TIPO_HEADER_CUSTOM:
                            foreach (var item in Headers["ListHeaders"])
                            {
                                httpWebRequest.Headers.Add(item["NombreHeader"].ToString(), item["ValorHeader"].ToString());
                            }

                            break;
                    }



                }
            }
            return httpWebRequest;
        }
    }
}
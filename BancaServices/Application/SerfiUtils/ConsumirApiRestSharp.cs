using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using RestSharp;

namespace BancaServices.Application.Services.SerfiUtils
{
    public static class ConsumirApiRestSharp
    {

        public const string TIPO_AUTORIZACION_BASIC = "Basic";
        public const string TIPO_AUTORIZACION_BEARER = "Bearer";
        public const string TIPO_HEADER_CUSTOM = "CUSTOM";
        public const string CONTENT_TYPE_JSON = "application/json";
        public const string CONTENT_TYPE_XFORM = "application/x-www-form-urlencoded";

        public static async Task<JObject> RestSharpApiSalidaJObject(string url, JObject datosEntrada, Method Metodo = Method.Post, string ContentType = "application/json", JObject Headers = null, int Timeout = 2000, NLog.ILogger logger = null, bool AddContentType = false)
        {
            JObject result = new JObject();
            try
            {
                using (RestClient resclient = new RestClient(new RestClientOptions(url) { MaxTimeout = Timeout }))
                {
                    RestResponse restResponse = null;

                    var req = new RestRequest
                    {
                        Method = Metodo
                    };
                    req.AddHeader("Accept", "application/json");

                    req.SetHeaderAdicional(Headers);

                    if (!Metodo.Equals(Method.Get))
                    {
                        req.AddHeader("Content-Type", ContentType);
                        req.AddStringBody(JsonConvert.SerializeObject(datosEntrada), DataFormat.Json);
                        restResponse = await resclient.ExecuteAsync(req);
                    }
                    else
                    {
                        if (AddContentType)
                        {
                            req.AddHeader("Content-Type", ContentType);
                        }
                        restResponse = await resclient.GetAsync(req);
                    }

                    if (restResponse.IsSuccessful || restResponse.StatusCode.ToString() == "OK")
                    {
                        if (restResponse.Content != null)
                        {
                            result = JObject.Parse(restResponse.Content);
                        }
                    }
                    else if (restResponse.ContentType != null && restResponse.ContentType.Equals(CONTENT_TYPE_JSON))
                    {
                        result = JObject.Parse(restResponse.Content);
                    }
                    else
                    {
                        result["Codigo"] = "02";
                        result["Descripcion"] = "Error " + restResponse.ErrorMessage;
                        result["CodigoHttp"] = (int)restResponse.StatusCode;
                        result["DescripcionHttp"] = restResponse.StatusDescription;
                    }
                    if (result.ToString().Contains("{},"))
                    {
                        string arreglaIBS = result.ToString().Replace("{},", "\"\",");
                        result = JObject.Parse(arreglaIBS);
                    }


                }
            }
            catch (Exception ex)
            {
                if (logger != null)
                {
                    string mensaje = ex.ToString();
                    if (ex.Message.Equals("Request aborted"))
                    {
                        mensaje = $"La url posiblemente superó el timeout de {Timeout} milisegundos {ex.ToString()}";
                    }
                    logger.Error("Error Consumiendo: {URL} Error: {ERROR}", url, mensaje);
                }

                result = new JObject();
            }

            return result;
        }

        public static async Task<JArray> RestSharpApiSalidaJArray(string url, JObject datosEntrada, Method Metodo = Method.Post, string ContentType = "application/json", JObject Headers = null, int Timeout = 2000, NLog.ILogger logger = null)
        {
            JArray result = new JArray();
            try
            {
                using (RestClient resclient = new RestClient(new RestClientOptions(url) { MaxTimeout = Timeout }))
                {
                    RestResponse restResponse = null;

                    var req = new RestRequest
                    {
                        Method = Metodo
                    };
                    req.AddHeader("Accept", "application/json");
                    req.AddHeader("Content-Type", ContentType);
                    req.SetHeaderAdicional(Headers);

                    if (!Metodo.Equals(Method.Get))
                    {
                        req.AddStringBody(JsonConvert.SerializeObject(datosEntrada), DataFormat.Json);
                        restResponse = await resclient.PostAsync(req);
                    }
                    else
                    {
                        restResponse = await resclient.GetAsync(req);
                    }

                    if (restResponse.IsSuccessful || restResponse.StatusCode.ToString() == "OK")
                    {
                        if (restResponse.Content != null)
                        {
                            result = JArray.Parse(restResponse.Content);
                        }
                    }
                    else if (restResponse.ContentType != null && restResponse.ContentType.Equals(CONTENT_TYPE_JSON))
                    {
                        result = JArray.Parse(restResponse.Content);
                    }
                    else
                    {
                        result["Codigo"] = "02";
                        result["Descripcion"] = "Error";
                        result["CodigoHttp"] = (int)restResponse.StatusCode;
                        result["DescripcionHttp"] = restResponse.StatusDescription;
                    }


                }
            }
            catch (Exception ex)
            {
                if (logger != null)
                {
                    string mensaje = ex.ToString();
                    if (ex.Message.Equals("Request aborted"))
                    {
                        mensaje = $"La url posiblemente superó el timeout de {Timeout} milisegundos {ex.ToString()}";
                    }
                    logger.Error("Error Consumiendo: {URL} Error: {ERROR}", url, mensaje);
                }

                result = new JArray();
            }
            return result;
        }

        public static void SetHeaderAdicional(this RestRequest client, JObject Headers)
        {

            if (Headers != null)
            {
                string autori;

                if (Headers["Tipo"] != null)
                {
                    string tipo = Headers["Tipo"].ToString();
                    switch (tipo)
                    {
                        case TIPO_AUTORIZACION_BASIC:
                            string encoded = Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(Headers["Usuario"].ToString() + ":" + Headers["Password"].ToString()));
                            autori = $"{TIPO_AUTORIZACION_BASIC} {encoded}";
                            client.AddHeader("Authorization", autori);
                            if (Headers.ContainsKey("ListHeaders") && Headers["ListHeaders"] != null)
                            {
                                foreach (var item in Headers["ListHeaders"])
                                {
                                    client.AddHeader(item["NombreHeader"].ToString(), item["ValorHeader"].ToString());
                                }
                            }
                            break;

                        case TIPO_AUTORIZACION_BEARER:
                            autori = $"{TIPO_AUTORIZACION_BEARER} {Headers["Token"].ToString()}";
                            client.AddHeader("Authorization", autori);
                            if (Headers.ContainsKey("ListHeaders") && Headers["ListHeaders"] != null)
                            {
                                foreach (var item in Headers["ListHeaders"])
                                {
                                    client.AddHeader(item["NombreHeader"].ToString(), item["ValorHeader"].ToString());
                                }
                            }
                            break;

                        case TIPO_HEADER_CUSTOM:
                            foreach (var item in Headers["ListHeaders"])
                            {
                                client.AddHeader(item["NombreHeader"].ToString(), item["ValorHeader"].ToString());
                            }
                            break;
                    }



                }


            }
        }
    }
}
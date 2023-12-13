using Newtonsoft.Json.Linq;

namespace BancaServices.Application.Services.SerfiUtils
{
    public static class ConexionBus
    {

        /// <summary>
        /// Permite armar el Header para consulta rel BUS
        /// </summary>
        /// <param name="NombreDestino"></param>
        /// <param name="NameSpace"></param>
        /// <param name="Operacion"></param>
        /// <returns></returns>
        public static JObject GenerarHeaderBus(string SystemId, string NombreDestino, string NameSpace, string Operacion)
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
            Header.Add("systemId", SystemId);
            Header.Add("messageId", messageId);
            Header.Add("invokerDateTime", invokerDateTime);
            Header.Add("securityCredential", "");
            Header.Add("destination", destination);


            return Header;

        }

        public static JObject GenerarRequestHeaderOut(JObject Header, JObject Body)
        {
            JObject requestHeaderOut = new JObject();
            requestHeaderOut.Add("requestHeaderOut", new JObject() {
                                                                                {"Header", Header },
                                                                                { "Body", Body }
                            });

            return requestHeaderOut;
        }

        public static JObject GenerarBody(string Operacion, JObject Body)
        {
            JObject Bod = new JObject
            {
                { Operacion, Body }
            };

            return Bod;
        }

    }
}
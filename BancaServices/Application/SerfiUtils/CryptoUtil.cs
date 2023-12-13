using NLog;
using System.Xml;

namespace BancaServices.Application.Services.SerfiUtils
{
    public class CryptoUtil
    {
        private readonly NLog.ILogger logger = LogManager.GetCurrentClassLogger();
        private readonly IConfiguration _configuration;


        public CryptoUtil(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public string EncryptData(string data)
        {
            string respuesta = string.Empty;
            string xmlSalida = string.Empty;
            string xml = string.Empty;
            try
            {
                CERPServiceRef.ServiceSoapClient cerpService = new CERPServiceRef.ServiceSoapClient();
                string interfaz = _configuration["INTERFAZ_ENCRIPTACION"];
                xml = string.Format("<transaccion><trn>E01</trn><int>{0}</int><data>{1}</data></transaccion>", interfaz, data);
                xmlSalida = cerpService.ProcessData(xml);
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xmlSalida);
                respuesta = doc.SelectSingleNode("/transaccion/response").InnerText.Trim();
            }
            catch (Exception ex)
            {
                logger.Error($"Error EncryptData: Request={xml} Response={xmlSalida}  Error={ex.ToString()}");
            }

            return respuesta;
        }

        public string DecryptData(string data)
        {
            string respuesta = string.Empty;
            string xmlSalida = string.Empty;
            string xml = string.Empty;
            try
            {
                CERPServiceRef.ServiceSoapClient cerpService = new CERPServiceRef.ServiceSoapClient();
                string interfaz = _configuration["INTERFAZ_ENCRIPTACION"];
                xml = string.Format("<transaccion><trn>E02</trn><int>{0}</int><cbc3des>{1}</cbc3des></transaccion>", interfaz, data);
                xmlSalida = cerpService.ProcessData(xml);
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xmlSalida);
                respuesta = doc.SelectSingleNode("/transaccion/response").InnerText.Trim();
            }
            catch (Exception ex)
            {
                logger.Error($"Error DecryptData: Request={xml} Response={xmlSalida}  Error={ex.ToString()}");
            }

            return respuesta;
        }

        public string EncryptPin(string tarjeta, string pin)
        {
            string respuesta = string.Empty;
            string xmlSalida = string.Empty;
            string xml = string.Empty;
            try
            {
                CERPServiceRef.ServiceSoapClient cerpService = new CERPServiceRef.ServiceSoapClient();
                string interfaz = _configuration["INTERFAZ_ENCRIPTACION"];
                xml = string.Format("<transaccion><trn>E03</trn><int>{0}</int><pin>{1}</pin><tarjeta>{2}</tarjeta></transaccion>", interfaz, pin, tarjeta);
                xmlSalida = cerpService.ProcessData(xml);
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xmlSalida);
                respuesta = doc.SelectSingleNode("/transaccion/ansipinblock").InnerText.Trim();
            }
            catch (Exception ex)
            {
                logger.Error($"Error EncryptPin: Request={xml} Response={xmlSalida}  Error={ex.ToString()}");
            }

            return respuesta;
        }
    }
}
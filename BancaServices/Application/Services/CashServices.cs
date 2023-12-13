using Newtonsoft.Json.Linq;
using BancaServices.Models;
using BancaServices.Domain.Interfaces;

namespace BancaServices.Application.Services
{
    public class CashServices : ICashServices
    {
        private readonly NLog.ILogger logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly IConfiguration _configuration;

        public CashServices(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public async Task<ResponseCashOutModel> CashOut(RequestCashOutModel request)
        {
            ResponseCashOutModel resultado;
            try
            {
                JObject req = JObject.FromObject(request);
                logger.Info("Request Genesys Cashout Req={REQ}", req.ToString());
                var response = await SerfiUtils.ConsumirApiRest.ConsumirApiSalidaJObject(_configuration["UrlCashOutNew"], req);
                logger.Info("Respuesta Genesys Cashout Resp={RESP}", response.ToString());
                resultado = Newtonsoft.Json.JsonConvert.DeserializeObject<ResponseCashOutModel>(response.ToString());
            }
            catch (Exception ex)
            {
                logger.Error<Exception>("Error Genesys Cashout Error={Error}", ex);
                return null;
            }
            return resultado;
        }

        public async Task<CashResponseModel> ChashOut(CashRequestModel request)
        {
            CashResponseModel resultado;
            try
            {
                JObject req = JObject.FromObject(request);
                logger.Info("Request Genesys Cashout Req={REQ}", req.ToString());
                var response = await SerfiUtils.ConsumirApiRest.ConsumirApiSalidaJObject(_configuration["urlCashOut"], req);
                logger.Info("Respuesta Genesys Cashout Resp={RESP}", response.ToString());
                resultado = Newtonsoft.Json.JsonConvert.DeserializeObject<CashResponseModel>(response.ToString());
            }
            catch (Exception ex)
            {
                logger.Error<Exception>("Error Genesys Cashout Error={Error}", ex);
                return null;
            }
            return resultado;
        }

        public async Task<ResponseTRNCodeModel> TransactionCodeTRN(RequestTRNCodeModel request)
        {
            ResponseTRNCodeModel resultado;
            try
            {
                JObject req = JObject.FromObject(request);
                logger.Info("Request Generar Codigo de Transaccion Req={REQ}", req.ToString());
                var response = await SerfiUtils.ConsumirApiRest.ConsumirApiSalidaJObject(_configuration["UrlTransactionCodeTRNCode"], req);
                logger.Info("Respuesta Generar Codigo de Transaccion Resp={RESP}", response.ToString());
                resultado = Newtonsoft.Json.JsonConvert.DeserializeObject<ResponseTRNCodeModel>(response.ToString());
            }
            catch (Exception ex)
            {
                logger.Error<Exception>("Error Generar Codigo de Transaccion Error={Error}", ex);
                return null;
            }
            return resultado;
        }

    }
}
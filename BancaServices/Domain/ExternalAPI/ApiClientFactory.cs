using Refit;

namespace BancaServices.ExternalAPI
{
    public static class ApiClientFactory
    {
        public static T Create<T>(IConfiguration configuration)
        {
            if (typeof(T) == typeof(ITransferenciaProtocolApi))
            {
                string urlTransferencia = configuration["Url_Transferencia"];
                return RestService.For<T>(urlTransferencia);
            }

            throw new InvalidOperationException();
        }
    }
}
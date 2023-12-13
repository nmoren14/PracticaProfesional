using BancaServices.Domain.Interfaces;
using BancaServices.ConsultaCDTServiceRef;
using System.ServiceModel;

namespace BancaServices.Application.Services
{
    public class ConsultarCDTClientFactoryService : IConsultarCDTClientFactory
    {
        public ConsultarCDTClient Create()
        {
            var endpoint = new EndpointAddress("https://localhost:44310/api/Producto/Productos");
            var binding = new BasicHttpBinding();
            var client = new ConsultarCDTClient(binding, endpoint);
            return client;
        }
    }
}

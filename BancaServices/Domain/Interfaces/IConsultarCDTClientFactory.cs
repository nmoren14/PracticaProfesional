using BancaServices.ConsultaCDTServiceRef;

namespace BancaServices.Domain.Interfaces
{
    public interface IConsultarCDTClientFactory
    {
        ConsultarCDTClient Create();
    }
}

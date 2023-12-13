using Newtonsoft.Json.Linq;
using BancaServices.Application.Services.SerfiUtils;

namespace BancaServices.Domain.Interfaces
{
    public interface ITransferenciasService
    {
        Task<JObject> EstadoTransaccion(string idCliente);
        Task<JObject> Transferir(JObject parameters, Utils.TiposTransferencias tipoTransferencia);

        Task<JObject> EstadoTransferencia(JObject d);
    }
}

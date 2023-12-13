using BancaServices.Models.CompraEnlinea;
using Newtonsoft.Json.Linq;

namespace BancaServices.Domain.Interfaces
{
    public interface IAvanceService
    {
        Task<CompraEnlineaResponse> CompraCretidoEnlinea(CompraEnlineaRequest req);
        bool PuedeAvanzar(string tipoId, string idCliente, string tarjeta, decimal montoAvanzar);
        Task<JObject> RealizarAvance(JObject parameters);
    }
}

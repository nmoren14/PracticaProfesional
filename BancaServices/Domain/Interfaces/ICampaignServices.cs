using BancaServices;
using Newtonsoft.Json.Linq;

namespace BancaServices.Domain.Interfaces
{
    public interface ICampaignServices
    {
        CampCuentasAhorros ConsultaClienteCampCuentaAhorros(string idClient, string tipoId);
        Task<JObject> ActualizarClienteCampCuentaAhorros(CampCuentasAhorros clineteCamp);
    }
}

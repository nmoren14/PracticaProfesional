using BancaServices;
using Newtonsoft.Json.Linq;

namespace BancaServices.Domain.Interfaces
{
    public interface IPseServices
    {
        Task<JObject> TransaccionPSE(TransaccionesPSE parameters);
    }
}

using Newtonsoft.Json.Linq;

namespace BancaServices.Domain.Interfaces
{
    public interface IActivacionServices
    {
        Task<JObject> HomologarTarjeta(JObject parameters);
    }
}

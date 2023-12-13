using Newtonsoft.Json.Linq;

namespace BancaServices.Domain.Interfaces
{
    public interface IRediferirServicesV2
    {
        Task<JObject> RediferirCliente(JObject data);
        Task<JObject> CalcularCuotaPorTarjeta(JObject data);
        Task<JObject> ValidarConsultaCMS(JArray data, string TipoId, string IdCliente);
        Task<JObject> apps(string TipoId, string IdCliente);
    }
}

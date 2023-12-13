using Newtonsoft.Json.Linq;
using BancaServices.Models.TarjetaVirtual;
using BancaServices;

namespace BancaServices.Domain.Interfaces
{
    public interface ITarjetaVirtualService
    {
        Task<JObject> RegistrarLog(TarjetaVirtualLog log, string NumTarjeta);
        Task<TarjetaVirtualLog> ConsultarLog(TarjetaVirtualRequest data);
        Task<JObject> ConsultaTarjetaVirtual(TarjetaVirtualRequest data);
    }
}
using BancaServices.Models.BLoqueoProducto;
using Newtonsoft.Json.Linq;

namespace BancaServices.Domain.Interfaces
{
    public interface IBloqueoServices
    {
        JObject BloquearTCO(JObject parameters);
        Task<JObject> BloquearUsuario(JObject data);
        Task<JObject> BloquearProductos(BloquearProducto producto);
        Task<JObject> TipoBloqueo(BloquearProducto producto);
        Task<JObject> BloquearCuentaAhorros(string TipoDocumento, string NumeroDocumento, string NumeroCuentaH);
    }
}

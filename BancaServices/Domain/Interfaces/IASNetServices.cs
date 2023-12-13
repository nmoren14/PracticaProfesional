using BancaServices.Application.Services;
using BancaServices.Models.Billetera;
using Newtonsoft.Json.Linq;

namespace BancaServices.Domain.Interfaces
{
    public interface IASNetServices
    {
        Task<double> ObtenerCupoProducto(string numeroProducto, ASNetServices.TipoProductos tipoProducto);

        Task<JObject> ValidacionSeguridad(BilleteraSeguridadRequest data);
    }
}

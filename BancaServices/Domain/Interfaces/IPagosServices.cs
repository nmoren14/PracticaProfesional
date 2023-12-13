using Newtonsoft.Json.Linq;
using BancaServices.PagoTcoAsNet;

namespace BancaServices.Domain.Interfaces
{
    public interface IPagosServices
    {
        Task<JObject> RealizarPago(JObject parameters);
        JObject ConsultarPagoPorTarjeta(string tarjeta);
        Task<JObject> ConsultarPagoPorPrestamo(string numPrestamo);
        Task<JObject> RealizarPagoPrestamo(JObject parameters);
        Task<ResponsePagoTarjetaCreditoDTO> PagoAutorizador(string numTarjeta, string idCliente, string tipoId, string monto);
    }
}

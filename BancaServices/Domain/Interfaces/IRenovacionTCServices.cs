using BancaServices.Models.RenovacionTC;

namespace BancaServices.Domain.Interfaces
{
    public interface IRenovacionTCServices
    {
        Task<List<TarjetaModelItem>> ConsultarRenovacion(string tipoId, string idCliente);
        Task<RespuestaRenovarTC> RenovarTC(RequestRenovarTC request);
        Task<RespuestaGenerarOTP> GenerarOTP(string tipoId, string idCliente, string tarjeta);
        Task<List<TarjetaRenovada>> ConsultarEstadosTarjetasRenovacion(string tipoId, string idCliente);
        Task<RespuestaCargueMasivo> RenovacionMasiva(int renovacionCargueId, int RenovacionLogId);
    }
}

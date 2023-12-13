using Refit;
using BancaServices.Models.DTO;

namespace BancaServices.ExternalAPI
{
    public interface ITransferenciaProtocolApi
    {
        [Post("/transaccionReverso")]
        Task<TransferenciasOtrosBancosResponse> Reversar([Body] TransferenciaOtrosBancosDTO request);
    }
}

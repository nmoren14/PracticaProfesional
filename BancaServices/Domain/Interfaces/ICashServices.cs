using BancaServices.Models;

namespace BancaServices.Domain.Interfaces
{
    public interface ICashServices
    {
        Task<CashResponseModel> ChashOut(CashRequestModel request);
        Task<ResponseTRNCodeModel> TransactionCodeTRN(RequestTRNCodeModel request);
        Task<ResponseCashOutModel> CashOut(RequestCashOutModel request);
    }
}

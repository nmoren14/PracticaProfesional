
namespace BancaServices.Models
{
    public class RequestTRNCodeModel
    {
        public String transactionCode { get; set; }
        public String environment { get; set; }
        public String creationUser { get; set; }
    }
}
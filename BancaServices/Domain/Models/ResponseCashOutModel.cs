
namespace BancaServices.Models
{
    public class ResponseCashOutModel
    {
        public Boolean successful { get; set; }
        public DataResponseCashOutModel data { get; set; }
        public String code { get; set; }
        public String message { get; set; }
        public String errorDetail { get; set; }
    }
}
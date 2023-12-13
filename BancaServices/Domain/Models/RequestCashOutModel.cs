
namespace BancaServices.Models
{
    public class RequestCashOutModel
    {
        public String environment { get; set; }
        public String trn { get; set; }
        public String codTrn { get; set; }
        public String codCnl { get; set; }
        public String codTmn { get; set; }
        public String ctaDes { get; set; }
        public String ctaHas { get; set; }
        public String transactionCode { get; set; }
        public String amount { get; set; }
        public String date { get; set; }
    }
}
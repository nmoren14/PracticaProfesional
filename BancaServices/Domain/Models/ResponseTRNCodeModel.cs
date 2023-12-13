using BancaServices.Models;

namespace BancaServices.Models
{
    public class ResponseTRNCodeModel
    {
        public String code { get; set; }
        public String description { get; set; }
        public DataResponseTRNCodeModel data { get; set; }
    }
}
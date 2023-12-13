using Newtonsoft.Json;
namespace BancaServices.Models.DTO
{
    public class ReporteAnualCostoDTO
    {
        [JsonProperty(PropertyName = "anio")]
        public int Anio { get; set; }

        [JsonProperty(PropertyName = "certificado")]
        public string Certificado { get; set; }
    }
}
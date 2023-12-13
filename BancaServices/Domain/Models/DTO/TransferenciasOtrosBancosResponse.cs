using Newtonsoft.Json;

namespace BancaServices.Models.DTO
{
    public class TransferenciasOtrosBancosResponse
    {
        [JsonProperty("estado")]
        public Boolean Estado { get; set; }
        [JsonProperty("data")]
        public string Data { get; set; }
    }
}
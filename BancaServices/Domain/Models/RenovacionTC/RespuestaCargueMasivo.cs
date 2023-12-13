using Newtonsoft.Json;
namespace BancaServices.Models.RenovacionTC
{
    public class RespuestaCargueMasivo
    {
        [JsonProperty("codigo")]
        public string Codigo { get; set; }
        [JsonProperty("descripcion")]
        public string Descripcion { get; set; }

        [JsonProperty("error")]
        public string Error { get; set; }

        [JsonProperty("estadoCargue")]
        public bool EstadoCargue { get; set; }

        public RespuestaCargueMasivo()
        {
            Descripcion = "Ha ocurrido un error, intente más tarde";
            Codigo = "01";
            EstadoCargue = false;
        }
    }
}
using Newtonsoft.Json;

namespace BancaServices.Models.RenovacionTC
{
    public class TarjetaModel
    {
        public string numProducto { get; set; }
        public string codProducto { get; set; }
        public string nomProducto { get; set; }
        public string tipoProducto { get; set; }
        public string categoria { get; set; }
        public string estado { get; set; }
        public string fechaEmision { get; set; }
        public string disponible { get; set; }
        public int saldo { get; set; }
    }
    public class TarjetaModelItem
    {
        public string Tarjeta { get; set; }
        public string FechaVencimiento { get; set; }
        public string EstadoTarjeta { get; set; }
    }

    public class TarjetaRenovada
    {
        [JsonProperty("tarjeta")]
        public string Tarjeta { get; set; }

        [JsonProperty("fechaActivacion")]
        public string FechaActivacion { get; set; }

        [JsonProperty("estadoTarjeta")]
        public string EstadoTarjeta { get; set; }

        [JsonProperty("medioRenovacion")]
        public string MedioRenovacion { get; set; }
    }

    public class RenovacionTarjeta
    {
        public string Tarjeta { get; set; }
        public string FechaVencimiento { get; set; }
        public string EstadoTarjeta { get; set; }
        public string EstadoRenovacion { get; set; }
        public string Cedula { get; set; }
    }
}
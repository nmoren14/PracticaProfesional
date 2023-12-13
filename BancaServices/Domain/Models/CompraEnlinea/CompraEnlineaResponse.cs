namespace BancaServices.Models.CompraEnlinea
{
    public class CompraEnlineaResponse
    {
        public string codigoRespuesta { get; set; }
        public string descripcionRespuesta { get; set; }
        public string detalleError { get; set; } = "";
        public string numeroAutorizacion { get; set; } = "";
        public string fechaAutorizacion { get; set; } = "";
        public string horaAutorizacion { get; set; } = "";

    }
}
namespace BancaServices.Models.Billetera
{
    public class BilleteraSeguridadResponse
    {
        public string numeroAutorizacion { get; set; }
        public string codigoRespuesta { get; set; }
        public string descripcionRespuesta { get; set; }
        public string consecutivo { get; set; }
        public string filler1 { get; set; }
        public string filler2 { get; set; }
        public string filler3 { get; set; }
    }
}
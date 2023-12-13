
namespace BancaServices.Models
{
    public class RequestCrearCTAH
    {
        public string tipoId { get; set; }
        public string numId { get; set; }
        public string codigoProducto { get; set; }
        public int firma { get; set; }
        public int monto { get; set; }
        public int cuota { get; set; }
        public string medioPago { get; set; }
        public string cuentaDebitar { get; set; }
        public string frecuencia { get; set; }
        public int plazo { get; set; }
        public int diaInicio { get; set; }
        public int mesInicio { get; set; }
        public int anoInicio { get; set; }
        public string condicion { get; set; }
    }
}
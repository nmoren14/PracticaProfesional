
namespace BancaServices.Models.DTO
{
    public class TransferenciaOtrosBancosDTO
    {
        public string identCliente { get; set; }
        public string tipoIdentCliente { get; set; }
        public string codBanco { get; set; }
        public string cuentaDestino { get; set; }
        public string tipoCtaDestino { get; set; }
        public string cuentaOrigen { get; set; }
        public string referencia { get; set; }
        public string valor { get; set; }
        public string canal { get; set; }
        public string ipCreacion { get; set; }
        public string codPais { get; set; }
        public string usuarioCreacion { get; set; }
        public Int64 retrieval { get; set; }

    }
}
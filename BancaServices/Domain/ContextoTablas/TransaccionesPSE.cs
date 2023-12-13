namespace BancaServices
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    public partial class TransaccionesPSE
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string TipoId { get; set; }
        public string IdCliente { get; set; }
        public int Canal { get; set; }
        public string Usuario { get; set; }
        public decimal Monto { get; set; }
        public string TipoCuenta { get; set; }
        public string NumeroCuenta { get; set; }
        public string CodigoPSE { get; set; }
        public string CodigoConvenio { get; set; }
        public string NombreComercio { get; set; }
        public string DescripcionPago { get; set; }
        public string Referencia1 { get; set; }
        public string Referencia2 { get; set; }
        public string Referencia3 { get; set; }
        public string IpCliente { get; set; }
        public System.DateTime Fecha { get; set; }
        public System.TimeSpan Hora { get; set; }
        public string CodigoRespuesta { get; set; }
        public string DescripcionRespuesta { get; set; }
    }
}

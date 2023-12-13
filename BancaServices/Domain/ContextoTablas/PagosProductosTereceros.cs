namespace BancaServices
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    public partial class PagosProductosTereceros
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string TipoIdCliente { get; set; }
        public string IdCliente { get; set; }
        public string TipoIdDeudor { get; set; }
        public string IdDeudor { get; set; }
        public string CuentaOrigen { get; set; }
        public string NumProducto { get; set; }
        public string TipoProducto { get; set; }
        public decimal Valor { get; set; }
        public string CodigoRespuesta { get; set; }
        public string DescripcionRespuesta { get; set; }
        public System.DateTime Fecha { get; set; }
        public string NumeroTrx { get; set; }
    }
}

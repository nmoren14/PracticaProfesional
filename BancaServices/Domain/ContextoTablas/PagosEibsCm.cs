namespace BancaServices
{    
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    public partial class PagosEibsCms
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string TipoId { get; set; }
        public string IdCliente { get; set; }
        public string NumProducto { get; set; }
        public string CuentaOrigen { get; set; }
        public Nullable<decimal> Monto { get; set; }
        public string NumTransaccion { get; set; }
        public string CodigoRespuesta { get; set; }
        public string DescripcionRespuesta { get; set; }
        public System.DateTime Fecha { get; set; }
        public System.TimeSpan Hora { get; set; }
        public string respuestaAuto { get; set; }
        public string numeroAutorizacionAuto { get; set; }
    }
}

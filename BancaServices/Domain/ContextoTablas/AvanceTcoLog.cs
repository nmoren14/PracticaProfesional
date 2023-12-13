namespace BancaServices
{    
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    public partial class AvanceTcoLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public decimal Id { get; set; }
        public string TipoIdCliente { get; set; }
        public string IdCliente { get; set; }
        public string Tarjeta { get; set; }
        public decimal Total { get; set; }
        public string NumeroReferencia { get; set; }
        public string Ip { get; set; }
        public string Canal { get; set; }
        public string CodigoRespuesa { get; set; }
        public string DescripcionRespuesta { get; set; }
        public string NumeroAutorizacion { get; set; }
        public System.DateTime Fecha { get; set; }
        public System.TimeSpan Hora { get; set; }
        public string CodRespuestaEibs { get; set; }
        public string DescRespuestaEibs { get; set; }
        public string NumeroTransaccion { get; set; }
        public Nullable<int> Cuotas { get; set; }
    }
}

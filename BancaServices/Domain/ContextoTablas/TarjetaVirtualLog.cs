namespace BancaServices
{ 
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    public partial class TarjetaVirtualLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string TipoIdCliente { get; set; }
        public string IdCliente { get; set; }
        public string Respuesta { get; set; }
        public System.DateTime Fecha { get; set; }
        public System.TimeSpan Hora { get; set; }
        public string Ip { get; set; }
    }
}

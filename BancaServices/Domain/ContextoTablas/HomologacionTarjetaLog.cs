namespace BancaServices
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    public partial class HomologacionTarjetaLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string IdCliente { get; set; }
        public string TipoId { get; set; }
        public string TarjetaAnterior { get; set; }
        public string TarjetaNueva { get; set; }
        public System.DateTime Fecha { get; set; }
        public System.TimeSpan Hora { get; set; }
        public string Estado { get; set; }
        public string EstadoRespuesta { get; set; }
        public int AutorizaMaster { get; set; }
        public string Ip { get; set; }
    }
}

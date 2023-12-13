namespace BancaServices
{    
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    public partial class RediferidosLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public Nullable<int> TipoId { get; set; }
        public Nullable<long> IdCliente { get; set; }
        public string Monto { get; set; }
        public string NumTarjeta { get; set; }
        public string IpCliente { get; set; }
        public Nullable<System.DateTime> FechaRegistro { get; set; }
        public string Respuesta { get; set; }
        public Nullable<int> Plazo { get; set; }
        public string Reestructurado { get; set; }
        public string Categoria { get; set; }
        public string DiasMora { get; set; }
    }
}

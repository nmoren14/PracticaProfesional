namespace BancaServices
{    
    using System.ComponentModel.DataAnnotations;
    public partial class RenovacionCargueLog
    {
        [Key]
        public long RenovacionCargueLogId { get; set; }
        public Nullable<long> RenovacionCargueId { get; set; }
        public string Usuario { get; set; }
        public string Ip { get; set; }
        public Nullable<System.DateTime> FechaRegistro { get; set; }
        public string EstadoCargue { get; set; }
    }
}

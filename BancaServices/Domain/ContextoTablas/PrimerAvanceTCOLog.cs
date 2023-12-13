namespace BancaServices
{    
    using System.ComponentModel.DataAnnotations;
    public partial class PrimerAvanceTCOLog
    {
        [Key]
        public int PrimerAvanceTCOLogId { get; set; }
        public string TipoDocumento { get; set; }
        public string NumeroDocumento { get; set; }
        public string Cuenta { get; set; }
        public string RespBloqueo { get; set; }
        public Nullable<System.DateTime> Fecha { get; set; }
        public Nullable<int> EstadoJob { get; set; }
        public string RespuestaJob { get; set; }
        public Nullable<int> IntentosDesbloqueo { get; set; }
    }
}

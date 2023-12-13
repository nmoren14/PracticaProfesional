namespace BancaServices
{    
    using System.ComponentModel.DataAnnotations;
    public partial class RenovacionMasivoLog
    {
        [Key]
        public int RenovacionMasivoLogId { get; set; }
        public string Proceso { get; set; }
        public string Request { get; set; }
        public string Response { get; set; }
        public Nullable<System.DateTime> Fecha { get; set; }
    }
}

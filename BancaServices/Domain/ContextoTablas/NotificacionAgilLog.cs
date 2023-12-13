namespace BancaServices
{
    using System.ComponentModel.DataAnnotations;
    public partial class NotificacionAgilLog
    {
        [Key]
        public int NotificacionAgilLogId { get; set; }
        public string Action { get; set; }
        public string Request { get; set; }
        public string Response { get; set; }
        public string Date { get; set; }
    }
}

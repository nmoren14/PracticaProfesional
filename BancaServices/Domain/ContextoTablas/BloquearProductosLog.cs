namespace BancaServices
{
    using System.ComponentModel.DataAnnotations;
    public partial class BloquearProductosLog
    {
        [Key]
        public long BloquearProductosLogId { get; set; }
        public string Action { get; set; }
        public string TypeCard { get; set; }
        public string Request { get; set; }
        public string Response { get; set; }
        public string Date { get; set; }
    }
}

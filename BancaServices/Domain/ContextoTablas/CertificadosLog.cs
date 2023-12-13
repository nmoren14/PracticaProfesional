namespace BancaServices
{
    using System.ComponentModel.DataAnnotations;
    public partial class CertificadosLog
    {
        [Key]
        public int CertificadosLogId { get; set; }
        public string Solicitud { get; set; }
        public string Respuesta { get; set; }
        public string FechaRegistro { get; set; }
        public string Ip { get; set; }
    }
}

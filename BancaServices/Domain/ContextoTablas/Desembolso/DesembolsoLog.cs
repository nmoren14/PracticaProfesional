using System.ComponentModel.DataAnnotations;

namespace BancaServices.ContextoTablas.Desembolso
{
    public class DesembolsoLog
    {
        [Key]
        public long DesembolsoLogId { get; set; }
        public string NombreOpcion { get; set; }
        public string TipoDocumento { get; set; }
        public string NumeroDocumento { get; set; }
        public string Solicitud { get; set; }
        public string Respuesta { get; set; }
        public DateTime? Fecha { get; set; } = DateTime.Now;
        public string Ip { get; set; }

        public DesembolsoLog(string nombreOpcion, string tipoDocumento, string numeroDocumento, string respuesta, string solicitud = "", string ip = "")
        {
            NombreOpcion = nombreOpcion;
            TipoDocumento = tipoDocumento;
            NumeroDocumento = numeroDocumento;
            Solicitud = solicitud;
            Respuesta = respuesta;
            Ip = ip;
        }
    }
}
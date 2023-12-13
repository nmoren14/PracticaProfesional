namespace BancaServices
{    
    using System.ComponentModel.DataAnnotations;
    public partial class CuerpoRenovacionCargue
    {
        [Key]
        public long CuerpoRenovacionCargueId { get; set; }
        public long RenovacionCargueId { get; set; }
        public string Cedula { get; set; }
        public string TipoDocumento { get; set; }
        public string Tarjeta { get; set; }
        public string Respuesta { get; set; }
        public Nullable<bool> Renovado { get; set; }
        public string DetalleError { get; set; }
        public Nullable<System.DateTime> FechaRegistro { get; set; }
        public string FechaRenovacion { get; set; }
    }
}

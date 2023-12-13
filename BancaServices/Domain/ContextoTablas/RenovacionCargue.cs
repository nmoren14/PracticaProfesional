namespace BancaServices
{    
    using System.ComponentModel.DataAnnotations;
    public partial class RenovacionCargue
    {
        [Key]
        public long RenovacionCargueId { get; set; }
        public string NombreArchivo { get; set; }
        public int NumeroRegistro { get; set; }
        public string Usuario { get; set; }
        public Nullable<System.DateTime> FechaRegistro { get; set; }
        public string EstadoCargue { get; set; }
        public int Renovadas { get; set; }
        public int NoRenovadas { get; set; }
        public string DetalleError { get; set; }
    }
}

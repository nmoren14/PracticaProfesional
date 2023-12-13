namespace BancaServices.ContextoTablas.Integrador
{
    
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public class ParametrosDomiciliacion
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
        public string Valor { get; set; }
        public bool EsNumerico { get; set; }
        public DateTime FechaCreacion { get; set; }
        public string UsuarioCreacion { get; set; }
        public DateTime FechaModificacion { get; set; }
        public string UsuarioModificacion { get; set; }
    }
}
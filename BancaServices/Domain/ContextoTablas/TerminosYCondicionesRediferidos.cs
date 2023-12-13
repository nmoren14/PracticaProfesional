
namespace BancaServices
{    
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    public partial class TerminosYCondicionesRediferidos
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public Nullable<int> NumeroCondicion { get; set; }
        public string Descripcion { get; set; }
        public Nullable<int> GrupoCondicion { get; set; }
        public Nullable<bool> Contenido { get; set; }
    }
}

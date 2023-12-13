namespace BancaServices
{    
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    public partial class UsuariosBloqueadosLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public Nullable<int> TipoId { get; set; }
        public string Documento { get; set; }
        public string Result { get; set; }
        public string Error { get; set; }
    }
}

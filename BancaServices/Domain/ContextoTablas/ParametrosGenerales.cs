namespace BancaServices
{    
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    public partial class ParametrosGenerales
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string NombreParametro { get; set; }
        public Nullable<int> ValorNumerico { get; set; }
        public string ValorString { get; set; }
    }
}

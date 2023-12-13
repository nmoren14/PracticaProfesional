namespace BancaServices
{    
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    public partial class Parametros
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public string DescripcionParametro { get; set; }
        public string ValorParametro { get; set; }
        public string Sistema { get; set; }
        public Nullable<bool> Lista { get; set; }
    }
}

namespace BancaServices
{    
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    public partial class RenovacionTC
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdRenovacionTC { get; set; }
        public string Result { get; set; }
        public Nullable<System.DateTime> Fecha { get; set; }
    }
}

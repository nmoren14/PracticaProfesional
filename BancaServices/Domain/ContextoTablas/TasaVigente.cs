namespace BancaServices
{    
    using System.ComponentModel.DataAnnotations;
    public partial class TasaVigente
    {
        [Key]
        public int Año { get; set; }
        public int Mes { get; set; }
        public Nullable<double> Tasa { get; set; }
    }
}

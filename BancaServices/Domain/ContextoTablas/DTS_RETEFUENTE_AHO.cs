namespace BancaServices
{    
    using System.ComponentModel.DataAnnotations;
    public partial class DTS_RETEFUENTE_AHO
    {
        [Key]
        public int ID { get; set; }
        public Nullable<decimal> ano { get; set; }
        public string nit { get; set; }
        public string tipoId { get; set; }
        public string cuenta { get; set; }
        public Nullable<decimal> saldo { get; set; }
        public Nullable<decimal> intereses { get; set; }
        public Nullable<decimal> retencion { get; set; }
        public Nullable<decimal> gmf { get; set; }
        public Nullable<decimal> baseGmf { get; set; }
        public string nomProducto { get; set; }
    }
}

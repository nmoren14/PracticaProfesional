namespace BancaServices
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    public partial class ClientesPeriodoGracia
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public string TipoId { get; set; }
        public string IdCliente { get; set; }
        public string Cuenta { get; set; }
        public string Grupo { get; set; }
    }
}

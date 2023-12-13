namespace BancaServices
{    
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    public partial class CampCuentasAhorros
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string TipoId { get; set; }
        public string IdCliente { get; set; }
        public string Estado { get; set; }
        public Nullable<System.DateTime> FechaCreacion { get; set; }
        public Nullable<System.DateTime> FechaModificacion { get; set; }
        public string Direccion { get; set; }
        public string Email { get; set; }
        public string CodigoRespEibs { get; set; }
        public string NumCuentaCreada { get; set; }
        public string Ip { get; set; }
    }
}

using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace BancaServices.ContextoTablas.Integrador
{
    public class entidades
    {
        [Key, Column(Order = 0)]
        public string codigo { get; set; }
        [Key, Column(Order = 1)]
        public short tipoEntidadCodigo { get; set; }
        public string codigoCms { get; set; }
        public string codigoIbs { get; set; }
        public string transito { get; set; }
        public string digitoChequeo { get; set; }
        public string codBancolombia { get; set; }
        public string nombre { get; set; }
        public string identificacion { get; set; }
        public bool entidadesConvenios { get; set; }
        public bool estado { get; set; }
        public string usuarioCreacion { get; set; }
        public DateTime fechaCreacion { get; set; }
        public string usuarioModificacion { get; set; }
        public DateTime fechaModificacion { get; set; }
    }
}
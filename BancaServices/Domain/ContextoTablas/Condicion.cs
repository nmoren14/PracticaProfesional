using System.ComponentModel.DataAnnotations;

namespace BancaServices
{
    public class Condicion
    {
        [Key]
        public int IdCondicion { get; set; }
        public string Operacion { get; set; }
        public string SubOperacion { get; set; }
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
        public string Parametro { get; set; }
        public string Comparador { get; set; }
        public string Valor { get; set; }
    }

}
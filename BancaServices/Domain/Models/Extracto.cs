
namespace BancaServices.Models
{
    public class Extracto
    {
        public Extracto()
        { }

        public string Numero { get; set; }
        public List<ExtractoDetalle> Detalle { get; set; }
    }


}
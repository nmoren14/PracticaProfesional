
namespace BancaServices.Models
{
    //@numProd', @tipProd, '@nombre', '01', '163', 1163, @doc, @tdoc, '@direc', '@ciudad', '@email', @tel, @fax, 3, 'ROTATIVO', @fech, @hora, 'SER01PRY', 0, '0', '', 0)";
    public class InscribirCRO
    {
        public DateTime fecha { get; set; }
        public string tipoId { get; set; }
        public string idCliente { get; set; }
        public string numProd { get; set; }
        public string tipoProd { get; set; }
        public string nombre { get; set; }
        public string direccion { get; set; }
        public string ciudad { get; set; }
        public string email { get; set; }
        public string telefono { get; set; }
        public string fax { get; set; }
    }
}
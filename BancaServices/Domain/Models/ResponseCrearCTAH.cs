
namespace BancaServices.Models
{
    public class ResponseCrearCTAH
    {
        public bool estado { get; set; }
        public Data data { get; set; }
        public class Data
        {
            public string codError { get; set; }
            public string error { get; set; }
            public string cuenta { get; set; }
            public string estadoCuenta { get; set; }
        }
    }
}
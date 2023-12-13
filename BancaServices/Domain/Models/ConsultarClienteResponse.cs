
namespace BancaServices.Models
{
    public class ConsultarClienteResponse
    {
        public bool estado { get; set; }
        public Data data { get; set; }
    }
    public class Cuenta
    {
        public int id { get; set; }
        public string tipoIdCliente { get; set; }
        public string idCliente { get; set; }
        public string nombreCompleto { get; set; }
        public string codProducto { get; set; }
        public string descripcion { get; set; }
        public string codBanco { get; set; }
        public string codSucursal { get; set; }
        public string fechaDesembolso { get; set; }
        public string fechaVencimiento { get; set; }
        public double valorDesembolso { get; set; }
        public double saldoCapital { get; set; }
        public double otrosSaldos { get; set; }
        public double pagoTotal { get; set; }
        public string vencimientoCuota { get; set; }
        public double valorCuotaCorriente { get; set; }
        public string codigoCliente { get; set; }
        public double valorCuotasVencidas { get; set; }
        public int plazo { get; set; }
        public string tipoCartera { get; set; }
        public string tipoTasa { get; set; }
        public double tasaMora { get; set; }
        public double tasaCuotaCorriente { get; set; }
        public int periodicidadPagos { get; set; }
    }
    public class Data
    {
        public Cuenta cuenta { get; set; }
    }
}
using Newtonsoft.Json.Linq;

namespace BancaServices.Domain.Interfaces
{
    public interface IProductosService
    {

        Task<JArray> ConsultaCdt(string idCliente);
        Task<JArray> ConsultaCMS(string tipoId, string idCliente, bool muestraBloqueo);
        Task<JArray> ConsultarPrestamos(string tipoId, string idCliente);
        Task<JArray> ConultaCuantas(string idCliente);
        Task<JArray> ConsultaCuentaCorriente(string tipoId, string idCliente, string operacion);
        Task<JObject> DetalleCMS(string tipoId, string idCliente, string numProd);
        Task<JObject> DetallePrestamo(string tipoId, string idCliente, string numProd);
        Task<bool> TienePeriodoGracia(string tarjeta);
        double PagoMinPeriodoGracia(string referencia);
        Task<JArray> ConsultarSaldosCTAH(string idCliente);
        Task<JArray> ConsultarSaldosCTA(string idCliente, string tipoId, string codProducto);
        Task<JArray> ConsultarSaldosCTAByNroCuenta(string idCliente, string tipoId, string nroCuenta, string codProducto);
        Task<JObject> ConsultaTipoCuenta(string NumCuenta);
        Task<JArray> ConsultarConvenioEfectivo(string tipoId, string idCliente);
    }

}

using BancaServices.Models;
using BancaServices.Models.Desembolsar;
using Newtonsoft.Json.Linq;

namespace BancaServices.Domain.Interfaces
{
    public interface ICROService
    {
        Task<JObject> CupoDisponible(string idCliente, string tipoDocumento);
        Task<double> CupoDisponiblePorNumProducto(string tipodId, string idCliente, string numeroProducto);
        Task<ResponseInscription> InscribirCuenta(string tipoId, string idCliente, JArray productos, Cliente cliente);
        Task<ResponseInscription> CrearCTAH(string tipoId, string idCliente);
        Task<JObject> ConsultarCR(string tipoId, string idCliente);
        Task<JObject> Desembolsar(DesembolsarRequest req);
        Task<JObject> DatosSeguridad(string tipoId, string idCliente, string tarjeta);
        Task<JObject> ConsultarCRPre(string tipoId, string idCliente);
    }
}

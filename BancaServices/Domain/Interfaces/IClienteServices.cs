using BancaServices.Models;

namespace BancaServices.Domain.Interfaces
{
    public interface IClienteServices
    {
        Cliente ConsultaClienteByDoc(string tipoId, string idCliente);
        Task<Cliente> ConsultaClienteEIBS(string tipoId, string idCliente);
        Task<Cliente> ConsultaClienteByDocIVR(string tipoId, string idCliente);
        Task<ClienteDet> ObtenerDetalleCliente(string tipoId, string idCliente);
    }
}

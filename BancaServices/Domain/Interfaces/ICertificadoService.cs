using BancaServices.Models.DTO;
using Newtonsoft.Json.Linq;





namespace BancaServices.Domain.Interfaces
{
    public interface ICertificadoService
    {
        Task<bool> EstaAlDia(string tipoId, string idCliente);
        Task<byte[]> CertificadoAlDia(string tipoId, string idCliente, string nombre, bool cifrado);
        byte[] CertificadoRetefuente(string tipoId, string idCliente, string nombre, string ano, bool cifrado);
        byte[] CertificadoRetefuenteCDT(string tipoId, string idCliente, string nombre, string ano, bool cifrado);
        Task<byte[]> ReferenciaBancaria(string tipoId, string idCliente, string nombre, bool cifrado);
        byte[] CertificadoRetencion(string tipoId, string idCliente, string nombre, string ano, bool cifrado);
        List<ReporteAnualCostoDTO> ReportesPorIdCliente(string tipoId, string idCliente);
        Task<byte[]> CertificadoPazYSalvo(string tipoId, string idCliente, string numObligacion);
        Task<byte[]> CertificadoCDT(string tipoId, string idCliente);
        Task<List<ReporteAnualCostoDTO>> GetRACByExtractos(string idCliente);
    }
}

using BancaServices.Models.ConsignarAcuenta;
using Newtonsoft.Json.Linq;

namespace BancaServices.Domain.Interfaces.ConsignarTransferir
{
    public interface IConsigarTransferir
    {
        Task<JObject> ConsignarACuenta(ConsignarAcuentaRequest parameters);
        Task<JObject> HacerCashin(ConsignarAcuentaRequest req);
        Task<JObject> ConsultarCuentasInscritas(string TipoDocumento, string NumeroDocumento, string CodigoPais = "CO", string CuentaAValidar = "");
    }
}
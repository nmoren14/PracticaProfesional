using Newtonsoft.Json.Linq;

namespace BancaServices.Domain.Interfaces
{
    public interface IConsultasCms
    {
        Task<JArray> ConsultaBinMastercard();
        Task<JObject> ConsultaDiaCms();
        Task<JArray> ConsultaBloqueoMora();
        Task<JArray> ConsultaBloqueoSobreCupo();
        Task<bool> ConsultaDiaFacturacion(string ciclo);
        Task<JArray> ConsultaCancelacionTarjeta();
        Task<JArray> ConsultaCastigosCartera();
        Task<JArray> ConsultaArregloCartera();
        Task<List<(string, string, string)>> ConsultaActivacionTarjeta();
        Task<JArray> ConsultaActivacionTarjetaTipos();
        Task<JArray> ConsultaInactivarCampanas(string cedula);
        Task<JObject> ConsultaAmparadas(string Tarjeta);
        Task<JObject> QueryCMS(JObject d);
        Task<bool> EsTarjetaEmpresarialPadre(string NumeroTarjeta);
        Task<string> ObtenerNumRotativoPorDocumento(string idCliente, string tipoDocumento);
        Task<string> ObtenerTarjetaPorNumRotativo(string numeroRotativo);
        Task<string> ObtenerReferenciaPorTarjetaoPorNumRotativo(string NumTarjetaORotativo);
        Task<string> ObtenerTipoProductoPorNumRotativoOTC(string NumTarjetaORotativo);
    }
}

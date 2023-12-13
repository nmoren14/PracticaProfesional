using Newtonsoft.Json;

namespace BancaServices.Models.Generic
{
    /// <summary>
    /// Ejemplo de uso CodigoDescripcionErrorData<Tipo de dato que quieres que sea data> variable= new();
    /// Ejemplo de uso CodigoDescripcionErrorData<JObject> variable= new();
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CodigoDescripcionErrorData<T> where T : class
    {
        [JsonProperty("Codigo")]
        public string Codigo { get; set; }

        [JsonProperty("Descripcion")]
        public string Descripcion { get; set; }

        [JsonProperty("DetalleError")]
        public string DetalleError { get; set; }

        [JsonProperty("Data")]
        public T Data { get; set; } = default(T);
    }
}
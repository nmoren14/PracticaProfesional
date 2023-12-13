using Newtonsoft.Json;

namespace BancaServices.Models.RenovacionTC
{
    public class RespuestaTartejasRenovadas
    {
        /// <summary>
        /// Codigo exitoso 00 - Codigo error 01
        /// </summary>
        [JsonProperty("codigo")]
        public string Codigo { get; set; }
        /// <summary>
        /// Listado de tarjetas del cliente renovacion
        /// </summary>
        /// 
        [JsonProperty("tarjetasRenovadas")]
        public List<TarjetaRenovada> TarjetasRenovadas { get; set; }
    }
    public class RespuestaTarjetaRenovada
    {
        /// <summary>
        /// Codigo exitoso 00 - Codigo error 01
        /// </summary>
        public string Codigo { get; set; }
        /// <summary>
        /// Descripcion del proceso
        /// </summary>
        public string Descripcion { get; set; }
    }

    public class RequestTarjetasRenovadas
    {
        /// <summary>
        /// Ultimos 4 digitos de la tarjeta
        /// </summary>
        public string tarjeta { get; set; }
        /// <summary>
        /// Cedula o identificacion del cliente
        /// </summary>
        public string idCliente { get; set; }
        /// <summary>
        /// id tipo de identificacion
        /// </summary>
        public string tipoId { get; set; }


    }
}
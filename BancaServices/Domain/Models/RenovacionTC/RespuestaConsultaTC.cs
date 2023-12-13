
namespace BancaServices.Models.RenovacionTC
{
    public class RespuestaConsultaTC
    {
        /// <summary>
        /// Codigo exitoso 00 - Codigo error 01
        /// </summary>
        public string Codigo { get; set; }
        /// <summary>
        /// Listado de tarjetas a renovar para el cliente
        /// </summary>
        public List<TarjetaModelItem> TarjetasRenovar { get; set; }
    }
    public class RespuestaRenovarTC
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
    public class RespuestaGenerarOTP
    {
        /// <summary>
        /// Codigo de respuesta 00 exitoso 01 error
        /// </summary>
        public string Codigo { get; set; }
        /// <summary>
        /// Celular del cliente al que se le envio el PIN
        /// </summary>
        public string Telefono { get; set; }
        /// <summary>
        /// PIN para validar
        /// </summary>
        public string PIN { get; set; }
    }
    public class RequestRenovarTC
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
        /// <summary>
        /// Canal que consume el servicio WEB o APP
        /// </summary>
        public string canal { get; set; }
        /// <summary>
        /// IP del cliente
        /// </summary>
        public string ip { get; set; }
    }
}
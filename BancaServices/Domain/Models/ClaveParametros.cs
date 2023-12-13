namespace BancaServices.Models
{
    public class ClaveParametros
    {
        public static readonly string TARJETA_CREDITO = "214";
        public static readonly string TARJETA_DEBITO = "213";
        public static readonly string CODIGO_ENTIDAD = "0423";

        public string TipoId { get; set; }
        public string TipoTarjeta { get; set; }
        public string Ip { get; set; }

        string identificacion;
        public string Identificacion
        {
            get
            {
                return identificacion;
            }

            set
            {
                identificacion = value;
            }
        }
        string tarjeta;

        public string Tarjeta
        {
            get
            {
                return tarjeta;
            }

            set
            {
                tarjeta = value;
            }
        }
        string pin;

        public string Pin
        {
            get
            {
                return pin;
            }

            set
            {
                pin = value;
            }
        }
        string pinNuevo;

        public string PinNuevo
        {
            get
            {
                return pinNuevo;
            }

            set
            {
                pinNuevo = value;
            }
        }

        public String Usuario { get; set; }

    }
}
namespace BancaServices.Models.CompraEnlinea
{
    public class CompraEnlineaRequest
    {

        public string tipoId { get; set; }
        public string idCliente { get; set; }

        public string numerodeCRO { get; set; } = ""; //llenar cuando se use CRO sino llenar el campo de numerotarjeta

        //REQ AsNetComprasSv CompraCreditoRequest_Dto
        public string codigoCanal { get; set; }
        public string codigoEntidad { get; set; }
        public string codigoSwitch { get; set; }
        public string pinblock { get; set; } = "";
        public string tipoProducto { get; set; }
        public string fechadeVencimiento { get; set; }//FORMATO AAMM
        public string filler1Track { get; set; } = "";
        public string filler2Track { get; set; } = "";
        public string cvc2 { get; set; }
        public string codigoDispositivo { get; set; } = "16";
        public string codTransaccion { get; set; }
        public string origenTransaccion { get; set; } = "N";
        public string modoEntradaPOS { get; set; } = "017";
        public string valorTransaccion { get; set; }
        public string descuento { get; set; } = "000";
        public string iva { get; set; }
        public string devolucion { get; set; } = "000";
        public string valorPropina { get; set; } = "000";
        public string numCuotas { get; set; }
        public string codigoEstablecimiento { get; set; } = "9999999998";
        public string codigoMCC { get; set; } = "5411";//Supermercados
        public string codConvenio { get; set; } = "35";
        public string canalNovedad { get; set; }
        public string terminalNovedadIp { get; set; }
        public string numReferencia { get; set; } = null;
        public string descripcionOperacion { get; set; } = "Avance a cuenta de ahorros";
        public string numeroAutorizacion { get; set; } = "";
        public string codigoCentroTUYA { get; set; } = "000";
        public string numeroTarjeta { get; set; } = "";
    }
}
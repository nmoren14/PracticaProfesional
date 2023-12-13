namespace BancaServices.Models.Billetera
{
    public class BilleteraSeguridadRequest
    {
        public string codigoAplicacion { get; set; }
        public string codigoProcesamiento { get; set; }
        public string fechaTransaccion { get; set; }
        public string horaTransaccion { get; set; }
        public string fechaEfectiva { get; set; }
        public string codigoCanal { get; set; }
        public string codigoSwitch { get; set; }
        public string codigoDispositivo { get; set; }
        public string consecutivo { get; set; }
        public string tipoTransaccion { get; set; }
        public string codigoEntidad { get; set; }
        public string numeroTarjeta { get; set; }
        public string userId { get; set; }
        public string passwordId { get; set; }
        public string numeroDocumento { get; set; }
        public string tipoDocumento { get; set; }
        public string fechaVencimientoTarjeta { get; set; }
        public string valorCVV2 { get; set; }
        public string valorTransaccion { get; set; }
        public string numeroCuotas { get; set; }
        public string estado { get; set; }
        public string pinblockActual { get; set; }
        public string pinblockNuevo { get; set; }
        public string filler1 { get; set; }
        public string filler2 { get; set; }
        public string filler3 { get; set; }
    }
}
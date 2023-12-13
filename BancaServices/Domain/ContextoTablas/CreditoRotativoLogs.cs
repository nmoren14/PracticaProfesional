namespace BancaServices
{    
    using System.ComponentModel.DataAnnotations;
    public partial class CreditoRotativoLogs
    {
        [Key]
        public int CreditoRotativoLogsId { get; set; }
        public string TipoId { get; set; }
        public string IdCliente { get; set; }
        public string MontoDesembolso { get; set; }
        public string NumCredito { get; set; }
        public string CodigoEstablecimeinto { get; set; }
        public string CodigoBanco { get; set; }
        public string CuentaDestino { get; set; }
        public string CodigoRespuesta { get; set; }
        public string DescripcionRespuesta { get; set; }
        public string CodigoAutorizador { get; set; }
        public string IdDesembolsoAutorizador { get; set; }
        public string CodigoCms { get; set; }
        public string IdDesembolsoCms { get; set; }
        public string CodigoCashIn { get; set; }
        public string NroTransaccionCashIn { get; set; }
        public DateTime? FechaRegistro { get; set; }
        public int? NumeroDeCuotas { get; set; }
    }
}

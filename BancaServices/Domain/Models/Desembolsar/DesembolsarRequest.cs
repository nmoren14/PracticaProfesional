namespace BancaServices.Models.Desembolsar
{
    public class DesembolsarRequest
    {
        //Transferencia
        public string tipoId { get; set; }
        public string idCliente { get; set; }
        public string Referencia { get; set; }

        public string IpCliente { get; set; }
        public string UsuarioCreacion { get; set; }

        //cashin


        //ambos
        public string montoDesembolo { get; set; }
        public bool EsCuentaExterna { get; set; }


        public string numCredito { get; set; }
        public string codigoEstablecimiento { get; set; }
        public string codigoBancoDestino { get; set; }
        public string cuentaDestino { get; set; } = string.Empty;
        public string TipoCuentaDestino { get; set; }
        public string CuentaOrigen { get; set; } = string.Empty;


        public string numCuotas { get; set; }
        public string canalNovedad { get; set; }


    }
}
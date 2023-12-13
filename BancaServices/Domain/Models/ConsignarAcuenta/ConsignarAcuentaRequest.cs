namespace BancaServices.Models.ConsignarAcuenta
{
    public class ConsignarAcuentaRequest
    {

        //Para decidir
        public bool EsCuentaExterna { get; set; }

        //para Cashin
        public string CodTRNCashin { get; set; }
        public string CodCanalCashin { get; set; }



        //Para Ambos
        public string CuentaDestino { get; set; }
        public string CuentaOrigen { get; set; } = string.Empty;
        public double MontoTotal { get; set; }


        //Para Transferencia
        public string IdCliente { get; set; }
        public string TipoId { get; set; }
        public string CodBanco { get; set; }
        public string TipoCuentaDestino { get; set; }
        public string CanalTransfer { get; set; } = string.Empty;
        public string Referencia { get; set; }
        public string Ip { get; set; }
        public string UsuarioCreacion { get; set; }
        public string CodPais { get; set; }


        /// <summary>
        /// BuildRequestCashin
        /// </summary>
        public ConsignarAcuentaRequest(double MontoTotal, string CtaDestino, string CodTRN = "", string CodCanal = "", string CuentaOrigen = "")
        {
            this.MontoTotal = MontoTotal;
            this.CuentaDestino = CtaDestino;
            this.CuentaOrigen = CuentaOrigen;

            EsCuentaExterna = false;
            CodTRNCashin = CodTRN;
            CodCanalCashin = CodCanal;
        }

        /// <summary>
        /// BuildRequestTransferencia
        /// </summary>
        public ConsignarAcuentaRequest(string IdCliente, string TipoId, string CodBanco, string TipoCuentaDestino, string CuentaDestino, double MontoTotal, string Referencia, string Ip, string UsuarioCreacion, string CodPais = "", string CanalTransfer = "", string CuentaOrigen = "")
        {
            this.IdCliente = IdCliente;
            this.TipoId = TipoId;
            this.CodBanco = CodBanco;

            this.CuentaDestino = CuentaDestino;
            this.TipoCuentaDestino = TipoCuentaDestino;
            this.CuentaOrigen = CuentaOrigen;

            this.MontoTotal = MontoTotal;
            this.CanalTransfer = CanalTransfer;
            this.Referencia = Referencia;

            this.Ip = Ip;
            this.CodPais = CodPais;
            this.UsuarioCreacion = UsuarioCreacion;

            EsCuentaExterna = true;
        }
    }
}
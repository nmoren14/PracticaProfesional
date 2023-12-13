namespace BancaServices
{
    public partial class DTS_RETEFUENTE
    {
        public decimal ano { get; set; }
        public string nit { get; set; }
        public string nombre { get; set; }
        public string obligacion { get; set; }
        public decimal saldo_capital { get; set; }
        public Nullable<decimal> intereses_pagados { get; set; }
        public Nullable<decimal> otros_pagados { get; set; }
        public Nullable<decimal> concepto_otros { get; set; }
        public string fecha_cargue { get; set; }
        public string tipo_credito { get; set; }
    }
}

namespace BancaServices
{ 
    using System.ComponentModel.DataAnnotations;
    public partial class DTS_RETEFUENTE_CDT
    {
        public Nullable<decimal> ano { get; set; }
        public Nullable<decimal> agencia { get; set; }
        public string nit { get; set; }
        public Nullable<decimal> saldo_capital { get; set; }
        public Nullable<decimal> interes_pagado { get; set; }
        public Nullable<decimal> intereses_causados { get; set; }
        public Nullable<decimal> retefuente_cobrada { get; set; }
        public Nullable<decimal> porcentaje_excento { get; set; }
        public string nombre { get; set; }

        [Key]
        public int ID { get; set; }
    }
}

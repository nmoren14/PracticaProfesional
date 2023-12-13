using Newtonsoft.Json;

namespace BancaServices.Models
{
    public class ConsultaParametrosResponse
    {
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
        [JsonProperty(PropertyName = "Data")]
        public Data data { get; set; }

        public class Data
        {
            [JsonProperty(PropertyName = "in_ban")]
            public In_Ban[] ban { get; set; }
            [JsonProperty(PropertyName = "in_cre")]
            public In_Cre[] cre { get; set; }
            [JsonProperty(PropertyName = "in_numCreditos")]
            public string numCreditos { get; set; }
            [JsonProperty(PropertyName = "in_numCuentas")]
            public string numCuentas { get; set; }
            [JsonProperty(PropertyName = "in_numDesembolso")]
            public string numDesembolso { get; set; }
            [JsonProperty(PropertyName = "in_res")]
            public In_Res[] res { get; set; }
        }

        public class In_Ban
        {
            public string codigoBanco { get; set; }
            public string cuentaDestino { get; set; }
            public string estadoCuenta { get; set; }
            public string nombreBanco { get; set; }
        }

        public class In_Cre
        {
            public string binDeCredito { get; set; }
            public string codigoTransac { get; set; }
            public string disponible { get; set; }
            public string montoMaximo { get; set; }
            public string montoMinimo { get; set; }
            public string motivoTransac { get; set; }
            public string numCredito { get; set; }
            public string origenTransac { get; set; }
            public string tipCredito { get; set; }
        }

        public class In_Res
        {
            public string AEstadoDesembolso { get; set; }
            public string AFechaDesembolso { get; set; }
            public string AValorDesembolso { get; set; }
            public Result result { get; set; }
        }

        public class Result
        {
            [JsonProperty(PropertyName = "in_codigoEstado")]
            public object codigoEstado { get; set; }
            [JsonProperty(PropertyName = "in_descripEstado")]
            public object descripEstado { get; set; }
            [JsonProperty(PropertyName = "in_estadoDesembolso")]
            public object estadoDesembolso { get; set; }
            [JsonProperty(PropertyName = "in_fechaCreacion")]
            public object fechaCreacion { get; set; }
        }
    }
}
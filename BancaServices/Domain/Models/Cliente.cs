using Newtonsoft.Json;

namespace BancaServices.Models
{
    public class Cliente
    {
        [JsonProperty("codigo")]
        public string Codigo { get; set; }

        [JsonProperty("descripcion")]
        public string Descripcion { get; set; }

        [JsonProperty("primerNombre")]
        public string PrimerNombre { get; set; }

        [JsonProperty("segundoNombre")]
        public string SegundoNombre { get; set; }

        [JsonProperty("primerApellido")]
        public string PrimerApellido { get; set; }

        [JsonProperty("segundoApellido")]
        public string SegundoApellido { get; set; }

        [JsonProperty("paisNacimiento")]
        public string PaisNacimiento { get; set; }

        [JsonProperty("estadoCivil")]
        public string EstadoCivil { get; set; }

        [JsonProperty("ocupacion")]
        public string Ocupacion { get; set; }

        [JsonProperty("estudios")]
        public string Estudios { get; set; }

        [JsonProperty("profesion")]
        public string Profesion { get; set; }

        [JsonProperty("actividadEconomica")]
        public string ActividadEconomica { get; set; }

        [JsonProperty("direccionResidencia")]
        public string DireccionResidencia { get; set; }

        [JsonProperty("barrioResidencia")]
        public string BarrioResidencia { get; set; }

        [JsonProperty("ciudadResidencia")]
        public string CiudadResidencia { get; set; }

        [JsonProperty("departamentoResidencia")]
        public string DepartamentoResidencia { get; set; }

        [JsonProperty("paisResidencia")]
        public string PaisResidencia { get; set; }

        [JsonProperty("telefonoPrincipal")]
        public string TelefonoPrincipal { get; set; }

        [JsonProperty("correoElectronico")]
        public string CorreoElectronico { get; set; }

        [JsonProperty("nombreEmpresa")]
        public string NombreEmpresa { get; set; }

        [JsonProperty("direccionEmpresa")]
        public string DireccionEmpresa { get; set; }

        [JsonProperty("telefonoEmpresa")]
        public string TelefonoEmpresa { get; set; }

        [JsonProperty("ingresos")]
        public string Ingresos { get; set; }

        [JsonProperty("egresos")]
        public string Egresos { get; set; }

        [JsonProperty("totalActivo")]
        public string TotalActivo { get; set; }

        [JsonProperty("totalPasivo")]
        public string TotalPasivo { get; set; }

        [JsonProperty("operMonedaExt")]
        public string OperMonedaExt { get; set; }

        [JsonProperty("estratoVivienda")]
        public string EstratoVivienda { get; set; }

        [JsonProperty("personasACargo")]
        public string PersonasACargo { get; set; }

        [JsonProperty("paisLabora")]
        public string PaisLabora { get; set; }

        [JsonProperty("departamentoLabora")]
        public string DepartamentoLabora { get; set; }

        [JsonProperty("ciudadLabora")]
        public string CiudadLabora { get; set; }

        [JsonProperty("cargo")]
        public string Cargo { get; set; }

        [JsonProperty("fechaIngreso")]
        public string FechaIngreso { get; set; }

        [JsonProperty("salario")]
        public string Salario { get; set; }

        [JsonProperty("otrosIngresos")]
        public string OtrosIngresos { get; set; }

        [JsonProperty("detalleOtrosIngresos")]
        public string DetalleOtrosIngresos { get; set; }

        [JsonProperty("tituloPregrado")]
        public string TituloPregrado { get; set; }

        [JsonProperty("patrimonio")]
        public string Patrimonio { get; set; }

        [JsonProperty("celular")]
        public string Celular { get; set; }

        [JsonProperty("estado")]
        public string Estado { get; set; } = "ACTIVO CMS";
    }
}
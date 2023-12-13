namespace BancaServices.Application.Services.SerfiUtils
{
    public class SerfinanzaUtils
    {
        public enum Sistema { EIBS, AUTO, CMS };
        public static string HomologarTipoId(string tipoId, Sistema sistema)
        {
            string tipoIdHomologado = string.Empty;
            if (sistema != Sistema.CMS)
            {
                switch (tipoId)
                {
                    case "1":
                        tipoIdHomologado = sistema.Equals(Sistema.EIBS) ? "CC" : "01";
                        break;
                    case "2":
                        tipoIdHomologado = sistema.Equals(Sistema.EIBS) ? "CE" : "03";
                        break;
                    case "3":
                        tipoIdHomologado = sistema.Equals(Sistema.EIBS) ? "NIT" : "02";
                        break;
                    case "4":
                        tipoIdHomologado = sistema.Equals(Sistema.EIBS) ? "TI" : "04";
                        break;
                    case "5":
                        tipoIdHomologado = sistema.Equals(Sistema.EIBS) ? "PAS" : "05";
                        break;
                    case "9":
                        tipoIdHomologado = sistema.Equals(Sistema.EIBS) ? "RCN" : "09";
                        break;
                }
            }
            else
            {
                switch (tipoId)
                {
                    case "CC":
                        tipoIdHomologado = "1";
                        break;
                    case "CE":
                        tipoIdHomologado = "2";
                        break;
                    case "NIT":
                        tipoIdHomologado = "3";
                        break;
                    case "TI":
                        tipoIdHomologado = "4";
                        break;
                    case "PAS":
                        tipoIdHomologado = "5";
                        break;
                    case "RCN":
                        tipoIdHomologado = "9";
                        break;



                }

            }

            return tipoIdHomologado;
        }
    }
}

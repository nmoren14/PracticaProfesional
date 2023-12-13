namespace BancaServices.Application.Services.SerfiUtils
{
    public static class NotificarErrorUtil
    {
        public static void NotificarError(string asunto, string mensaje, IConfiguration configuration)
        {

            string[] Correos = configuration["Email_Alertamiento"]?.ToString().Split(',');

            if (Correos != null && Correos.Any())
            {
                foreach (var item in Correos)
                {
                    ComunicacionUtil.EnviarEmail(configuration, item, mensaje, "BancaServices - " + asunto, false);
                }

            }
        }

    }
}
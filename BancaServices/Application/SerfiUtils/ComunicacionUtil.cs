using NLog;
using System.Net;
using System.Net.Mail;

namespace BancaServices.Application.Services.SerfiUtils
{
    public static class ComunicacionUtil
    {
        private static readonly NLog.ILogger logger = LogManager.GetCurrentClassLogger();

        public class RespEnviarEmail
        {
            public bool Enviado { get; set; }
            public string Descripcion { get; set; }
        }

        public static RespEnviarEmail EnviarEmail(IConfiguration configuration, string email, string body, string asunto, bool isHtml, byte[] sendFile = null, string nameFile = "")
        {
            RespEnviarEmail resp = new RespEnviarEmail();
            resp.Enviado = true;
            resp.Descripcion = "Correo Enviado";

            try
            {
                // Accede a la configuración utilizando IConfiguration
                string fromEmail = configuration["SmtpSettings:From"];
                string smtpHost = configuration["SmtpSettings:Host"];
                int smtpPort = int.Parse(configuration["SmtpSettings:Port"]);
                string smtpUsername = configuration["SmtpSettings:Username"];
                string smtpPassword = configuration["SmtpSettings:Password"];

                if (!Utils.IsValidEmail(email))
                {
                    resp.Enviado = false;
                    resp.Descripcion = "El correo No es valido";
                    return resp;
                }

                MailMessage mail = new MailMessage(fromEmail, email);
                using (SmtpClient smtpClient = new SmtpClient
                {
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Host = smtpHost,
                    Port = smtpPort,
                    Credentials = new NetworkCredential(smtpUsername, smtpPassword)
                })
                {
                    mail.Body = body;
                    mail.IsBodyHtml = isHtml;
                    mail.Subject = asunto;
                    if (sendFile != null)
                    {
                        mail.Attachments.Add(new Attachment(new MemoryStream(sendFile), nameFile));
                    }
                    smtpClient.Send(mail);
                }


            }
            catch (SmtpException ex)
            {
                logger.Error("Error EnviarEmail:  Error={Error} ", ex);
                resp.Enviado = false;
                resp.Descripcion = "Error Enviando Correo";
            }

            return resp;

        }

    }
}
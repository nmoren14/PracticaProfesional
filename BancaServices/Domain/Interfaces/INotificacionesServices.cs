namespace BancaServices.Domain.Interfaces
{
    public interface INotificacionesServices
    {
        Task EnviarSmsAsync(string celular, string mensaje);
        bool sendEmailCertificado(string email, string body, string asunto, bool isHtml, byte[] sendFile, string nameFile);

    }
}

namespace BancaServices.Domain.Interfaces
{
    public interface IAgilService
    {
        Task NotificacionAgilSMS(string tipoId, string idCliente, string cuenta);

    }
}

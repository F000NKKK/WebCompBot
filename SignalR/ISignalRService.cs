using WebCompBot.RabbitMq;

namespace WebCompBot.SignalR
{
    public interface ISignalRService
    {
        Task SendMessageAsync(RabbitMqBackgroundService.Message message);
    }
}

using System.Threading;
using System.Threading.Tasks;

namespace WebCompBot.RabbitMq
{
    public interface IRabbitMqBackgroundService
    {
        // Метод для обработки сообщений
        Task ProcessMessagesAsync(CancellationToken cancellationToken);
    }
}

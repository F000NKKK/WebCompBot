namespace WebCompBot.RabbitMq
{
    public interface IRabbitMqService
    {
        // Метод для отправки сообщения в очередь
        void SendMessageToQueue(string message);
        // Метод для подтверждения обработки сообщения
        Task AcknowledgeMessage(ulong deliveryTag);
        // Асинхронный метод для отклонения сообщения
        Task RejectMessage(ulong deliveryTag, bool requeue);
        // Метод для обработки сообщений
        Task ProcessMessagesAsync(CancellationToken cancellationToken);

    }
}


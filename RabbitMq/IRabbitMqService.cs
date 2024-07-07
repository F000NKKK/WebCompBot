namespace WebCompBot.RabbitMq
{
    public interface IRabbitMqService
    {
        // Метод для отправки сообщения в очередь
        void SendMessageToQueue(string message);
        // Метод для подтверждения обработки сообщения
        void AcknowledgeMessage(ulong deliveryTag);
        // Метод для отклонения сообщения
        void RejectMessage(ulong deliveryTag, bool requeue);
    }
}


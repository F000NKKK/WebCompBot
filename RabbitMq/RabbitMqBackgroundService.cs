using RabbitMQ.Client; // Используется для работы с RabbitMQ клиентом
using RabbitMQ.Client.Events; // Используется для обработки событий, связанных с RabbitMQ
using System.Text; // Предоставляет классы для работы с кодировками
using System.Text.Json; // Предоставляет методы для сериализации и десериализации JSON
using NLog; // Пространство имен для работы с NLog
using ILogger = NLog.ILogger; // Уточните, что это NLog.ILogger
using WebCompBot.SignalR;

namespace WebCompBot.RabbitMq
{
    public class RabbitMqBackgroundService : BackgroundService, IRabbitMqService, IRabbitMqBackgroundService
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger(); // Создание экземпляра логгера

        private readonly IConnection _connection; // Поле для хранения соединения с RabbitMQ
        private readonly IModel _channel; // Поле для хранения канала для общения с RabbitMQ
        private readonly ISignalRService _signalRBackgroundService;

        private const string PreProcessorQueueName = "PreProcessorQueue"; // Константа для имени очереди PreProcessor
        private const string FromPostProcessor = "WebCompBotQueue"; // Константа для имени очереди PostProcessor
        private const string FilePath = "uData/chatHistory.json"; // Константа для пути к файлу с историей чата

        // Конструктор класса
        public RabbitMqBackgroundService()
        {
            var factory = new ConnectionFactory() { HostName = "localhost" }; // Создание фабрики соединений с указанием хоста
            _connection = factory.CreateConnection(); // Создание соединения
            _channel = _connection.CreateModel(); // Создание канала

            // Объявление очереди PreProcessorQueue
            _channel.QueueDeclare(queue: PreProcessorQueueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
            // Объявление очереди FromPostProcessor
            _channel.QueueDeclare(queue: FromPostProcessor, durable: true, exclusive: false, autoDelete: false, arguments: null);

            _channel.BasicQos(0, 1, false); // Установка Quality of Service для канала
        }

        // Метод для отправки сообщения в очередь
        public void SendMessageToQueue(string message)
        {
            var body = Encoding.UTF8.GetBytes(message); // Кодирование сообщения в байты
            _channel.BasicPublish(exchange: "", routingKey: PreProcessorQueueName, basicProperties: null, body: body); // Публикация сообщения в очередь
            Logger.Info("Сообщение отправлено в PreProcessorQueue"); // Логирование отправки сообщения
        }

        public async Task AcknowledgeMessage(ulong deliveryTag)
        {
            await Task.Run(() => _channel.BasicAck(deliveryTag, false));
            Logger.Info($"Сообщение с deliveryTag {deliveryTag} подтверждено"); // Логирование подтверждения сообщения
        }

        public async Task RejectMessage(ulong deliveryTag, bool requeue)
        {
            await Task.Run(() => _channel.BasicReject(deliveryTag, requeue));
            Logger.Warn($"Сообщение с deliveryTag {deliveryTag} отклонено (requeue={requeue})"); // Логирование отклонения сообщения
        }

        // Переопределение метода ExecuteAsync для выполнения задач в фоновом режиме
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await ProcessMessagesAsync(stoppingToken); // Вызов метода для обработки сообщений
        }

        // Метод для обработки сообщений
        public async Task ProcessMessagesAsync(CancellationToken cancellationToken)
        {
            var consumer = new EventingBasicConsumer(_channel); // Создание потребителя сообщений

            // Обработка события получения сообщения
            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray(); // Получение тела сообщения
                var message = Encoding.UTF8.GetString(body); // Декодирование сообщения
                var deserializedMessage = JsonSerializer.Deserialize<Message>(message); // Десериализация сообщения в объект Message

                var flag = (deserializedMessage.Content != null) ? "NotNull" : "Null";
                Logger.Info($"\nПолучено сообщение:\nId: {deserializedMessage.Id}\nContent: {flag}\nMessageCurrentTime: {deserializedMessage.MessageCurrentTime}"); // Логирование получения сообщения

                try
                {
                    try
                    {
                        // Обработка сообщения
                        Logger.Info($"Обработка сообщения с ID: {deserializedMessage.Id}");

                        await _signalRBackgroundService.SendMessageAsync(deserializedMessage);

                        await AcknowledgeMessage(ea.DeliveryTag);
                    }
                    catch (Exception ex)
                    {
                        // Обработка исключений при обработке сообщения
                        Logger.Error(ex, $"Произошла ошибка при обработке сообщения с ID: {deserializedMessage.Id}");

                        // Отклонение сообщения и его повторная отправка в очередь
                        await RejectMessage(ea.DeliveryTag, true);
                    }
                }
                catch (TaskCanceledException ex)
                {
                    // Обработка отмены задачи
                    Logger.Warn(ex, "Задача отменена");
                }
                catch (Exception ex)
                {
                    // Обработка других исключений
                    Logger.Error(ex, "Произошла ошибка");
                }
            };

            _channel.BasicConsume(queue: FromPostProcessor, autoAck: false, consumer: consumer); // Начало потребления сообщений из очереди

            // Ожидание отмены задачи
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }

        // Метод для освобождения ресурсов
        public override void Dispose()
        {
            _channel?.Close(); // Закрытие канала
            _connection?.Close(); // Закрытие соединения
            Logger.Info("Ресурсы RabbitMQ закрыты"); // Логирование освобождения ресурсов
            base.Dispose(); // Вызов базового метода Dispose
        }

        // Класс для представления сообщения.
        public class Message
        {
            public string Id { get; set; } = string.Empty; // Идентификатор сообщения
            public string Content { get; set; } = string.Empty; // Содержимое сообщения
            public string MessageCurrentTime { get; set; } = string.Empty; // Время отправки сообщения
            public Boolean IsUserMessage { get; set; } = true; // Флаг User/Bot, True/False соответственно
        }
    }
}

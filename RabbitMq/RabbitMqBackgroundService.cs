using RabbitMQ.Client; // Используется для работы с RabbitMQ клиентом
using RabbitMQ.Client.Events; // Используется для обработки событий, связанных с RabbitMQ
using System.Text; // Предоставляет классы для работы с кодировками
using System.Text.Json; // Предоставляет методы для сериализации и десериализации JSON
using WebCompBot.RabbitMq; // Пространство имен для работы с RabbitMQ в проекте WebCompBot

// Определяет класс, который наследуется от BackgroundService и реализует интерфейсы IRabbitMqService и IRabbitMqBackgroundService
public class RabbitMqBackgroundService : BackgroundService, IRabbitMqService, IRabbitMqBackgroundService
{
    private readonly IConnection _connection; // Поле для хранения соединения с RabbitMQ
    private readonly IModel _channel; // Поле для хранения канала для общения с RabbitMQ

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
        Console.WriteLine($"[x] Отправлено {message} в {PreProcessorQueueName}"); // Логирование отправленного сообщения
    }

    // Метод для подтверждения обработки сообщения
    public void AcknowledgeMessage(ulong deliveryTag)
    {
        _channel.BasicAck(deliveryTag, false); // Подтверждение обработки сообщения
    }

    // Метод для отклонения сообщения
    public void RejectMessage(ulong deliveryTag, bool requeue)
    {
        _channel.BasicNack(deliveryTag, false, requeue); // Отклонение сообщения с возможностью его повторной отправки в очередь
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

            Console.WriteLine($"Получено сообщение: {message}"); // Логирование полученного сообщения

            try
            {
                try
                {
                    UpdateMessageInJsonFile(deserializedMessage); // Обновление сообщения в файле
                    // Логирование и подтверждение обработки сообщения
                    Console.WriteLine($"[x] Обработано сообщение: {JsonSerializer.Serialize(message)}");
                    AcknowledgeMessage(ea.DeliveryTag);
                }
                catch (Exception ex)
                {
                    // Обработка исключений при обработке сообщения
                    Console.WriteLine($"Произошла ошибка: {ex.Message}");

                    // Отклонение сообщения и его повторная отправка в очередь
                    RejectMessage(ea.DeliveryTag, true);
                }
            }
            catch (TaskCanceledException ex)
            {
                // Обработка отмены задачи
                Console.WriteLine($"Задача отменена: {ex.Message}");
            }
            catch (Exception ex)
            {
                // Обработка других исключений
                Console.WriteLine($"Произошла ошибка: {ex.Message}");
            }
        };

        _channel.BasicConsume(queue: FromPostProcessor, autoAck: false, consumer: consumer); // Начало потребления сообщений из очереди

        // Ожидание отмены задачи
        await Task.Delay(Timeout.Infinite, cancellationToken);
    }

    // Метод для обновления сообщения в JSON файле
    private void UpdateMessageInJsonFile(Message message)
    {
        var chatHistory = new Dictionary<string, List<Message>>(); // Создание словаря для хранения истории чатов

        if (File.Exists(FilePath)) // Проверка существования файла
        {
            var existingData = File.ReadAllText(FilePath); // Чтение данных из файла
            chatHistory = JsonSerializer.Deserialize<Dictionary<string, List<Message>>>(existingData) ?? new Dictionary<string, List<Message>>(); // Десериализация данных в словарь
        }

        var userMessages = chatHistory[message.Id.Split('$')[1]]; // Получение сообщений пользователя
        var existingMessage = userMessages.FirstOrDefault(m => m.Id == message.Id); // Поиск сообщения по идентификатору
        if (existingMessage != null)
        {
            existingMessage.AnswerContent = message.AnswerContent; // Обновление содержимого ответа
        }


        File.WriteAllText(FilePath, JsonSerializer.Serialize(chatHistory)); // Сохранение обновленной истории чатов в файл
    }

    // Метод для освобождения ресурсов
    public override void Dispose()
    {
        _channel?.Close(); // Закрытие канала
        _connection?.Close(); // Закрытие соединения
        base.Dispose(); // Вызов базового метода Dispose
    }

    // Вспомогательный класс для представления сообщения
    private class Message
    {
        public string Id { get; set; } = string.Empty; // Идентификатор сообщения
        public string Content { get; set; } = string.Empty; // Содержимое сообщения
        public string AnswerContent { get; set; } = string.Empty; // Содержимое ответа
    }
}
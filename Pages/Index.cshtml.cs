using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using WebCompBot.RabbitMq;
using ILogger = NLog.ILogger;

namespace WebCompBot.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;  // Логгер для логирования сообщений
        private readonly IRabbitMqService _rabbitMqService; // Поле для хранения сервиса RabbitMQ

        public IndexModel(IRabbitMqService rabbitMqService, ILogger<IndexModel> logger)
        {
            _rabbitMqService = rabbitMqService; // Инициализация поля сервиса RabbitMQ
            _logger = logger; // Инициализация логгера
        }

        [BindProperty]
        public MessageOnU MessageOnU { get; set; } = new(""); // Свойство для привязки данных формы

        // Класс для представления сообщения.
        public class Message
        {
            public string Id { get; set; } = string.Empty; // Идентификатор сообщения
            public string Content { get; set; } = string.Empty; // Содержимое сообщения
            public string MessageCurrentTime { get; set; } = string.Empty; // Время отправки сообщения
            public bool IsUserMessage { get; set; } = true; // Флаг User/Bot, True/False соответственно
        }

        public List<Message> MessageHistory { get; set; } = new List<Message>(); // История сообщений

        // Метод для обработки GET-запросов
        public void OnGet()
        {
            var username = Request.Cookies["UserLoginCookie"]; // Получение имени пользователя из cookie
            if (!string.IsNullOrEmpty(username))
            {
                var chatHistoryPath = Path.Combine(Environment.CurrentDirectory, "uData/chatHistory.json");
                if (System.IO.File.Exists(chatHistoryPath))
                {
                    try
                    {
                        var chatHistory = JsonSerializer.Deserialize<Dictionary<string, List<Message>>>(System.IO.File.ReadAllText(chatHistoryPath)); // Чтение истории чата из файла

                        if (chatHistory != null && chatHistory.ContainsKey(username))
                        {
                            MessageHistory = chatHistory[username]; // Загрузка истории сообщений пользователя
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Ошибка при чтении истории чата из файла.");
                        // Опционально: Можно установить MessageHistory как пустой список или показать сообщение об ошибке пользователю
                        MessageHistory = new List<Message>();
                    }
                }
            }
            else
            {
                Response.Redirect("/Login"); // Перенаправление на страницу логина, если пользователь не аутентифицирован
            }
        }

        // Метод для обработки POST-запросов
        public async Task<IActionResult> OnPostAsync()
        {
            var username = Request.Cookies["UserLoginCookie"]; // Получение имени пользователя из cookie
            if (!string.IsNullOrEmpty(username))
            {
                if (!string.IsNullOrEmpty(MessageOnU.message))
                {
                    var chatHistoryPath = Path.Combine(Environment.CurrentDirectory, "uData/chatHistory.json");
                    var chatHistory = System.IO.File.Exists(chatHistoryPath) ?
                        JsonSerializer.Deserialize<Dictionary<string, List<Message>>>(System.IO.File.ReadAllText(chatHistoryPath)) :
                        new Dictionary<string, List<Message>>();

                    if (chatHistory.ContainsKey(username))
                    {
                        MessageHistory = chatHistory[username]; // Загрузка истории сообщений пользователя
                    }
                    else
                    {
                        MessageHistory = new List<Message>();
                    }

                    var newId = MessageHistory.Any() ?
                        ((MessageHistory.Max(m => Convert.ToUInt32(m.Id.Split("$")[0])) + 1) + "$" + username) :
                        (0 + "$" + username); // Создание нового сообщения с уникальным идентификатором

                    var sendObject = new Message
                    {
                        Content = MessageOnU.message,
                        Id = newId,
                        MessageCurrentTime = DateTime.Now.ToString("dd:MM:yyyy HH:mm:ss"),
                        IsUserMessage = true // Установите значение IsUserMessage как true для новых сообщений пользователя
                    };

                    MessageHistory.Add(sendObject); // Добавление нового сообщения в историю

                    try
                    {
                        // Отправляем объект в очередь PreProcessor
                        _rabbitMqService.SendMessageToQueue(JsonSerializer.Serialize(sendObject));
                        _logger.LogInformation($"Сообщение отправлено в очередь PreProcessor с ID '{sendObject.Id}' для пользователя '{username}'.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Ошибка при отправке сообщения в очередь RabbitMQ.");
                        return RedirectToPage(); // Перенаправление на ту же страницу в случае ошибки
                    }

                    chatHistory[username] = MessageHistory; // Обновление истории чата пользователя

                    try
                    {
                        // Сохранение обновленной истории чата в файл
                        System.IO.File.WriteAllText(chatHistoryPath, JsonSerializer.Serialize(chatHistory));
                        _logger.LogInformation($"История чата обновлена для пользователя '{username}'.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Ошибка при сохранении истории чата в файл.");
                    }

                    return RedirectToPage(); // Перенаправление на ту же страницу после успешной отправки сообщения
                }

                return RedirectToPage(); // Перенаправление на ту же страницу, если сообщение пустое
            }

            return RedirectToPage("/Login"); // Перенаправление на страницу логина, если пользователь не аутентифицирован
        }
    }

    // Класс для представления данных сообщения, введенного пользователем
    public record class MessageOnU(string message);
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace WebCompBot.Pages
{
    public class IndexModel : PageModel
    {
        [BindProperty]
        public MessageOnU MessageOnU { get; set; } = new(""); // Свойство для привязки данных формы

        private readonly RabbitMqBackgroundService _rabbitMqService; // Поле для хранения сервиса RabbitMQ

        public IndexModel(RabbitMqBackgroundService rabbitMqService)
        {
            _rabbitMqService = rabbitMqService; // Инициализация поля сервиса RabbitMQ
        }

        // Класс для представления сообщения.
        public class Message
        {
            public string Id { get; set; } = string.Empty; // Идентификатор сообщения
            public string Content { get; set; } = string.Empty; // Содержимое сообщения
            public string AnswerContent { get; set; } = string.Empty; // Содержимое ответа
        }

        public List<Message> MessageHistory { get; set; } = new List<Message>(); // История сообщений

        // Свойство для текущего времени
        public string CurrentTime { get; set; } = DateTime.Now.ToString("HH:mm:ss");

        // Метод для обработки GET-запросов
        public void OnGet()
        {
            var username = Request.Cookies["UserLoginCookie"]; // Получение имени пользователя из cookie
            if (!string.IsNullOrEmpty(username))
            {
                var chatHistoryPath = Path.Combine(Environment.CurrentDirectory, "uData/chatHistory.json");
                if (System.IO.File.Exists(chatHistoryPath))
                {
                    var chatHistory = JsonSerializer.Deserialize<Dictionary<string, List<Message>>>(System.IO.File.ReadAllText(chatHistoryPath)); // Чтение истории чата из файла

                    if (chatHistory != null && chatHistory.ContainsKey(username))
                    {
                        MessageHistory = chatHistory[username]; // Загрузка истории сообщений пользователя
                    }
                }

                // Обновление времени на странице
                CurrentTime = DateTime.Now.ToString("HH:mm:ss");
            }
            else
            {
                Response.Redirect("/Login"); // Перенаправление на страницу логина, если пользователь не аутентифицирован
            }
        }

        // Метод для обработки POST-запросов
        public async Task<IActionResult> OnPost()
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
                        Id = newId
                    };

                    MessageHistory.Add(sendObject); // Добавление нового сообщения в историю

                    // Отправляем объект в очередь PreProcessor
                    _rabbitMqService.SendMessageToQueue(message: JsonSerializer.Serialize(sendObject));

                    chatHistory[username] = MessageHistory; // Обновление истории чата пользователя
                    System.IO.File.WriteAllText(chatHistoryPath, JsonSerializer.Serialize(chatHistory)); // Сохранение обновленной истории чата в файл

                    return RedirectToPage(); // Перенаправление на ту же страницу
                }

                return RedirectToPage(); // Перенаправление на ту же страницу, если сообщение пустое
            }

            return RedirectToPage("/Login"); // Перенаправление на страницу логина, если пользователь не аутентифицирован
        }
    }

    // Класс для представления данных сообщения, введенного пользователем
    public record class MessageOnU(string message);
}

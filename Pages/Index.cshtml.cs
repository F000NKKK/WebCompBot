using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using WebCompBot.RabbitMq;
using ILogger = NLog.ILogger;

namespace WebCompBot.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;  // ������ ��� ����������� ���������
        private readonly IRabbitMqService _rabbitMqService; // ���� ��� �������� ������� RabbitMQ

        public IndexModel(IRabbitMqService rabbitMqService, ILogger<IndexModel> logger)
        {
            _rabbitMqService = rabbitMqService; // ������������� ���� ������� RabbitMQ
            _logger = logger; // ������������� �������
        }

        [BindProperty]
        public MessageOnU MessageOnU { get; set; } = new(""); // �������� ��� �������� ������ �����

        // ����� ��� ������������� ���������.
        public class Message
        {
            public string Id { get; set; } = string.Empty; // ������������� ���������
            public string Content { get; set; } = string.Empty; // ���������� ���������
            public string MessageCurrentTime { get; set; } = string.Empty; // ����� �������� ���������
            public bool IsUserMessage { get; set; } = true; // ���� User/Bot, True/False ��������������
        }

        public List<Message> MessageHistory { get; set; } = new List<Message>(); // ������� ���������

        // ����� ��� ��������� GET-��������
        public void OnGet()
        {
            var username = Request.Cookies["UserLoginCookie"]; // ��������� ����� ������������ �� cookie
            if (!string.IsNullOrEmpty(username))
            {
                var chatHistoryPath = Path.Combine(Environment.CurrentDirectory, "uData/chatHistory.json");
                if (System.IO.File.Exists(chatHistoryPath))
                {
                    try
                    {
                        var chatHistory = JsonSerializer.Deserialize<Dictionary<string, List<Message>>>(System.IO.File.ReadAllText(chatHistoryPath)); // ������ ������� ���� �� �����

                        if (chatHistory != null && chatHistory.ContainsKey(username))
                        {
                            MessageHistory = chatHistory[username]; // �������� ������� ��������� ������������
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "������ ��� ������ ������� ���� �� �����.");
                        // �����������: ����� ���������� MessageHistory ��� ������ ������ ��� �������� ��������� �� ������ ������������
                        MessageHistory = new List<Message>();
                    }
                }
            }
            else
            {
                Response.Redirect("/Login"); // ��������������� �� �������� ������, ���� ������������ �� ����������������
            }
        }

        // ����� ��� ��������� POST-��������
        public async Task<IActionResult> OnPostAsync()
        {
            var username = Request.Cookies["UserLoginCookie"]; // ��������� ����� ������������ �� cookie
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
                        MessageHistory = chatHistory[username]; // �������� ������� ��������� ������������
                    }
                    else
                    {
                        MessageHistory = new List<Message>();
                    }

                    var newId = MessageHistory.Any() ?
                        ((MessageHistory.Max(m => Convert.ToUInt32(m.Id.Split("$")[0])) + 1) + "$" + username) :
                        (0 + "$" + username); // �������� ������ ��������� � ���������� ���������������

                    var sendObject = new Message
                    {
                        Content = MessageOnU.message,
                        Id = newId,
                        MessageCurrentTime = DateTime.Now.ToString("dd:MM:yyyy HH:mm:ss"),
                        IsUserMessage = true // ���������� �������� IsUserMessage ��� true ��� ����� ��������� ������������
                    };

                    MessageHistory.Add(sendObject); // ���������� ������ ��������� � �������

                    try
                    {
                        // ���������� ������ � ������� PreProcessor
                        _rabbitMqService.SendMessageToQueue(JsonSerializer.Serialize(sendObject));
                        _logger.LogInformation($"��������� ���������� � ������� PreProcessor � ID '{sendObject.Id}' ��� ������������ '{username}'.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "������ ��� �������� ��������� � ������� RabbitMQ.");
                        return RedirectToPage(); // ��������������� �� �� �� �������� � ������ ������
                    }

                    chatHistory[username] = MessageHistory; // ���������� ������� ���� ������������

                    try
                    {
                        // ���������� ����������� ������� ���� � ����
                        System.IO.File.WriteAllText(chatHistoryPath, JsonSerializer.Serialize(chatHistory));
                        _logger.LogInformation($"������� ���� ��������� ��� ������������ '{username}'.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "������ ��� ���������� ������� ���� � ����.");
                    }

                    return RedirectToPage(); // ��������������� �� �� �� �������� ����� �������� �������� ���������
                }

                return RedirectToPage(); // ��������������� �� �� �� ��������, ���� ��������� ������
            }

            return RedirectToPage("/Login"); // ��������������� �� �������� ������, ���� ������������ �� ����������������
        }
    }

    // ����� ��� ������������� ������ ���������, ���������� �������������
    public record class MessageOnU(string message);
}

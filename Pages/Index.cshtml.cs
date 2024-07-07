using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace WebCompBot.Pages
{
    public class IndexModel : PageModel
    {
        [BindProperty]
        public MessageOnU MessageOnU { get; set; } = new(""); // �������� ��� �������� ������ �����

        private readonly RabbitMqBackgroundService _rabbitMqService; // ���� ��� �������� ������� RabbitMQ

        public IndexModel(RabbitMqBackgroundService rabbitMqService)
        {
            _rabbitMqService = rabbitMqService; // ������������� ���� ������� RabbitMQ
        }

        // ����� ��� ������������� ���������.
        public class Message
        {
            public string Id { get; set; } = string.Empty; // ������������� ���������
            public string Content { get; set; } = string.Empty; // ���������� ���������
            public string AnswerContent { get; set; } = string.Empty; // ���������� ������
        }

        public List<Message> MessageHistory { get; set; } = new List<Message>(); // ������� ���������

        // �������� ��� �������� �������
        public string CurrentTime { get; set; } = DateTime.Now.ToString("HH:mm:ss");

        // ����� ��� ��������� GET-��������
        public void OnGet()
        {
            var username = Request.Cookies["UserLoginCookie"]; // ��������� ����� ������������ �� cookie
            if (!string.IsNullOrEmpty(username))
            {
                var chatHistoryPath = Path.Combine(Environment.CurrentDirectory, "uData/chatHistory.json");
                if (System.IO.File.Exists(chatHistoryPath))
                {
                    var chatHistory = JsonSerializer.Deserialize<Dictionary<string, List<Message>>>(System.IO.File.ReadAllText(chatHistoryPath)); // ������ ������� ���� �� �����

                    if (chatHistory != null && chatHistory.ContainsKey(username))
                    {
                        MessageHistory = chatHistory[username]; // �������� ������� ��������� ������������
                    }
                }

                // ���������� ������� �� ��������
                CurrentTime = DateTime.Now.ToString("HH:mm:ss");
            }
            else
            {
                Response.Redirect("/Login"); // ��������������� �� �������� ������, ���� ������������ �� ����������������
            }
        }

        // ����� ��� ��������� POST-��������
        public async Task<IActionResult> OnPost()
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
                        Id = newId
                    };

                    MessageHistory.Add(sendObject); // ���������� ������ ��������� � �������

                    // ���������� ������ � ������� PreProcessor
                    _rabbitMqService.SendMessageToQueue(message: JsonSerializer.Serialize(sendObject));

                    chatHistory[username] = MessageHistory; // ���������� ������� ���� ������������
                    System.IO.File.WriteAllText(chatHistoryPath, JsonSerializer.Serialize(chatHistory)); // ���������� ����������� ������� ���� � ����

                    return RedirectToPage(); // ��������������� �� �� �� ��������
                }

                return RedirectToPage(); // ��������������� �� �� �� ��������, ���� ��������� ������
            }

            return RedirectToPage("/Login"); // ��������������� �� �������� ������, ���� ������������ �� ����������������
        }
    }

    // ����� ��� ������������� ������ ���������, ���������� �������������
    public record class MessageOnU(string message);
}

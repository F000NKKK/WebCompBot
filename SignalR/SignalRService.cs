using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using WebCompBot.RabbitMq;

namespace WebCompBot.SignalR
{
    public class SignalRService : ISignalRService
    {
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly ILogger<SignalRService> _logger;
        private readonly string _chatHistoryPath = Path.Combine("uData", "chatHistory.json"); // Обновленный путь к файлу

        public SignalRService(IHubContext<ChatHub> hubContext, ILogger<SignalRService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task SendMessageAsync(RabbitMqBackgroundService.Message message)
        {
            var chatHistory = LoadChatHistory();

            var user = message.Id.Split("#")[0].Split("$")[1];

            if (!chatHistory.ContainsKey(user))
            {
                chatHistory[user] = new List<RabbitMqBackgroundService.Message>();
            }

            chatHistory[user].Add(message);

            SaveChatHistory(chatHistory);

            await _hubContext.Clients.All.SendAsync("ReceiveMessage", user, message);
        }

        private Dictionary<string, List<RabbitMqBackgroundService.Message>> LoadChatHistory()
        {
            if (!File.Exists(_chatHistoryPath))
            {
                return new Dictionary<string, List<RabbitMqBackgroundService.Message>>();
            }

            var json = File.ReadAllText(_chatHistoryPath);
            return JsonSerializer.Deserialize<Dictionary<string, List<RabbitMqBackgroundService.Message>>>(json);
        }

        private void SaveChatHistory(Dictionary<string, List<RabbitMqBackgroundService.Message>> chatHistory)
        {
            var json = JsonSerializer.Serialize(chatHistory, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_chatHistoryPath, json);
        }
    }
}

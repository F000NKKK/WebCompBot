using Microsoft.AspNetCore.SignalR;
using WebCompBot.RabbitMq;
using System.Text.Json;

namespace WebCompBot.SignalR
{
    public class ChatHub : Hub
    {
        private readonly ISignalRService _signalRService;
        private readonly string _chatHistoryPath = Path.Combine("uData", "chatHistory.json"); // Обновленный путь к файлу
        public ChatHub(ISignalRService signalRService)
        {
            _signalRService = signalRService;
        }

        public async Task SendMessage(RabbitMqBackgroundService.Message message)
        {
            await _signalRService.SendMessageAsync(message);
        }
        public async Task<string> GetMessageContent(string requestMessageId, string user)
        {


            var chatHistory = LoadChatHistory();
            // Замените этот пример логики на реальную
            var message = chatHistory[user].FirstOrDefault(m => m.Id.Split('#')[0].Split('$')[0] == requestMessageId);
            if (message == null)
            {
                return "Not Found";
            }
            return message.Content;
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
    }
}


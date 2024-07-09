using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using WebCompBot.RabbitMq;

namespace WebCompBot.SignalR
{
    public class SignalRService : ISignalRService
    {
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly ILogger<SignalRService> _logger;
        private readonly string _chatHistoryPath;

        public SignalRService(IHubContext<ChatHub> hubContext, ILogger<SignalRService> logger, IWebHostEnvironment environment)
        {
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _chatHistoryPath = "uData/chatHistory.json";
        }

        public async Task SendMessageAsync(RabbitMqBackgroundService.Message message)
        {
            if (message == null)
            {
                _logger.LogError("Получено сообщение с null значением");
                return;
            }

            if (string.IsNullOrEmpty(message.Id))
            {
                _logger.LogError("Получено сообщение с пустым или null ID");
                return;
            }

            var chatHistory = LoadChatHistory();
            var user = message.Id.Split("#")[0].Split("$")[1];

            if (string.IsNullOrEmpty(user))
            {
                _logger.LogError("Не удалось извлечь пользователя из сообщения с ID: {MessageId}", message.Id);
                return;
            }

            if (!chatHistory.ContainsKey(user))
            {
                chatHistory[user] = new List<RabbitMqBackgroundService.Message>();
            }

            chatHistory[user].Add(message);

            SaveChatHistory(chatHistory);

            try
            {
                await _hubContext.Clients.All.SendAsync("ReceiveMessage", user, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отправке сообщения через SignalR");
            }
        }

        private Dictionary<string, List<RabbitMqBackgroundService.Message>> LoadChatHistory()
        {
            try
            {
                if (!File.Exists(_chatHistoryPath))
                {
                    return new Dictionary<string, List<RabbitMqBackgroundService.Message>>();
                }

                var json = File.ReadAllText(_chatHistoryPath);
                return JsonSerializer.Deserialize<Dictionary<string, List<RabbitMqBackgroundService.Message>>>(json)
                       ?? new Dictionary<string, List<RabbitMqBackgroundService.Message>>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при загрузке истории чата");
                return new Dictionary<string, List<RabbitMqBackgroundService.Message>>();
            }
        }

        private void SaveChatHistory(Dictionary<string, List<RabbitMqBackgroundService.Message>> chatHistory)
        {
            try
            {
                var json = JsonSerializer.Serialize(chatHistory, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_chatHistoryPath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при сохранении истории чата");
            }
        }
    }
}

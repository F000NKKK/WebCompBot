using Microsoft.AspNetCore.SignalR;
using WebCompBot.RabbitMq;
using System.Text.Json;

namespace WebCompBot.SignalR
{
    public class ChatHub : Hub
    {
        private readonly ISignalRService _signalRService;
        private readonly string _chatHistoryPath;
        private readonly ILogger<ChatHub> _logger;
        public ChatHub(ISignalRService signalRService, IWebHostEnvironment environment)
        {
            _signalRService = signalRService ?? throw new ArgumentNullException(nameof(signalRService));
            _chatHistoryPath = Path.Combine(environment.ContentRootPath, "uData", "chatHistory.json");
        }

        public async Task SendMessage(RabbitMqBackgroundService.Message message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            _signalRService.SendMessageAsync(message);
        }

        public async Task<string> GetMessageContent(string requestMessageId, string user)
        {
            if (string.IsNullOrEmpty(requestMessageId) || string.IsNullOrEmpty(user))
            {
                return "Invalid Request";
            }

            try
            {
                var chatHistory = LoadChatHistory();
                var message = chatHistory.GetValueOrDefault(user)?.FirstOrDefault(m => m.Id.Split('#')[0] == requestMessageId);

                if (chatHistory == null)
                {
                    return "Not Found Chat History";
                }

                if (message == null)
                {
                    return "Not Found Message";
                }

                return message.Content;
            }
            catch (Exception ex)
            {
                // Логирование ошибки и возвращение сообщения о внутренней ошибке
                _logger.LogError(ex, "Ошибка при получении содержимого сообщения с ID: {RequestMessageId}", requestMessageId);
                return "Error";
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
    }
}

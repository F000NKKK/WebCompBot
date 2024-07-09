using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using static WebCompBot.Pages.IndexModel;

namespace WebCompBot.Controllers
{
    [Route("JsonUpdate")]
    public class JsonUpdateController : Controller
    {
        private readonly ILogger<JsonUpdateController> _logger;

        public JsonUpdateController(ILogger<JsonUpdateController> logger)
        {
            _logger = logger;
        }

        // Метод для получения данных JSON
        [HttpGet("GetJsonData")]
        public IActionResult GetJsonData()
        {
            try
            {
                _logger.LogInformation("Начало получения данных JSON.");

                // Путь к файлу JSON
                var chatHistoryPath = Path.Combine(Environment.CurrentDirectory, "uData/chatHistory.json");

                // Чтение истории чата из файла
                var chatHistoryJson = System.IO.File.ReadAllText(chatHistoryPath);
                var chatHistory = JsonSerializer.Deserialize<Dictionary<string, List<Message>>>(chatHistoryJson);

                _logger.LogInformation("Данные JSON успешно получены.");

                // Возвращаем данные в формате JSON
                return Json(chatHistory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении данных JSON.");

                // В случае ошибки возвращаем статус 500
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}

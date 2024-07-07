using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using static WebCompBot.Pages.IndexModel;


namespace WebCompBot.Controllers
{
    [Route("JsonUpdate")]
    public class JsonUpdateController : Controller
    {
        // Метод для получения данных JSON
        [HttpGet("GetJsonData")]
        public IActionResult GetJsonData()
        {
            try
            {
                // Путь к файлу JSON
                var chatHistoryPath = Path.Combine(Environment.CurrentDirectory, "uData/chatHistory.json");

                // Чтение истории чата из файла
                var chatHistoryJson = System.IO.File.ReadAllText(chatHistoryPath);
                var chatHistory = JsonSerializer.Deserialize<Dictionary<string, List<Message>>>(chatHistoryJson);

                // Возвращаем данные в формате JSON
                return Json(chatHistory);
            }
            catch (Exception ex)
            {
                // В случае ошибки возвращаем статус 500
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}

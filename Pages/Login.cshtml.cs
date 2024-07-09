using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace WebCompBot.Pages
{
    public class LoggingModel : PageModel
    {
        private readonly ILogger<LoggingModel> _logger;

        [BindProperty]
        public LoggedInUser LoggedInUser { get; set; } = new("", "");

        public LoggingModel(ILogger<LoggingModel> logger)
        {
            _logger = logger;
        }

        // Метод для обработки POST-запросов
        public void OnPost()
        {
            _logger.LogInformation("Получен запрос на вход для пользователя: {Username}", LoggedInUser.Username);

            try
            {
                var users = JsonSerializer.Deserialize<List<User>>(System.IO.File.ReadAllText("uData/logging.json"));

                var user = users.FirstOrDefault(u => u.Username == LoggedInUser.Username && u.Password == LoggedInUser.Password);

                if (user != null)
                {
                    Response.Cookies.Append(key: "UserLoginCookie", value: LoggedInUser.Username);
                    _logger.LogInformation("Пользователь {Username} успешно аутентифицирован.", LoggedInUser.Username);
                    Response.Redirect("/");
                }
                else
                {
                    _logger.LogWarning("Аутентификация не удалась для пользователя: {Username}", LoggedInUser.Username);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Произошла ошибка при обработке запроса на вход для пользователя: {Username}", LoggedInUser.Username);
            }
        }
    }

    // Класс для представления данных пользователя
    public record class LoggedInUser(string Username, string Password) { }

    public class User
    {
        public required string Username { get; set; }
        public required string Password { get; set; }
    }
}


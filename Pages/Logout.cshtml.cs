using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace WebCompBot.Pages
{
    public class LogoutModel : PageModel
    {
        private readonly ILogger<LogoutModel> _logger;

        public LogoutModel(ILogger<LogoutModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            var username = Request.Cookies["UserLoginCookie"]; // Получение имени пользователя из cookie
            if (string.IsNullOrEmpty(username))
            {
                _logger.LogWarning("Попытка выхода без авторизации.");
                Response.Redirect("/Login"); // Перенаправление на страницу логирования.
            }
            else
            {
                _logger.LogInformation("Пользователь {Username} перешел на страницу выхода.", username);
            }
        }

        // Метод для обработки POST-запросов
        public void OnPost()
        {
            var username = Request.Cookies["UserLoginCookie"];
            Response.Cookies.Delete(key: "UserLoginCookie"); // Удаление cookie для аутентифицированного пользователя.

            if (username != null)
            {
                _logger.LogInformation("Пользователь {Username} успешно вышел.", username);
            }
            else
            {
                _logger.LogWarning("Попытка выхода без существующей сессии.");
            }

            Response.Redirect("/Login"); // Перенаправление на страницу логирования.
        }
    }
}

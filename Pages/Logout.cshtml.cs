using Microsoft.AspNetCore.Mvc; // Пространство имен для создания контроллеров и действий
using Microsoft.AspNetCore.Mvc.RazorPages; // Пространство имен для работы с Razor Pages
using System.Text.Json; // Пространство имен для работы с JSON

namespace WebCompBot.Pages
{
    public class LogoutModel : PageModel
    {
        public void OnGet()
        {
            var username = Request.Cookies["UserLoginCookie"]; // Получение имени пользователя из cookie
            if (string.IsNullOrEmpty(username))
            {
                Response.Redirect("/Login"); // Перенаправление на страницу логирования.
            }
        }

        // Метод для обработки POST-запросов
        public void OnPost()
        {
            Response.Cookies.Delete(key: "UserLoginCookie"); // Установка cookie для аутентифицированного пользователя.

            Response.Redirect("/Login"); // Перенаправление на страницу логирования.
        }
    }
}

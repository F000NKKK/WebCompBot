using Microsoft.AspNetCore.Mvc; // Пространство имен для создания контроллеров и действий
using Microsoft.AspNetCore.Mvc.RazorPages; // Пространство имен для работы с Razor Pages
using System.Text.Json; // Пространство имен для работы с JSON

namespace WebCompBot.Pages
{
    public class LoggingModel : PageModel
    {
        [BindProperty]
        public LoggedInUser LoggedInUser { get; set; } = new("", "");

        // Метод для обработки POST-запросов
        public void OnPost()
        {
            // Чтение пользователей из файла logging.json.
            var users = JsonSerializer.Deserialize<List<User>>(System.IO.File.ReadAllText("uData/logging.json"));

            // Проверка наличия пользователя с указанными именем и паролем.
            var user = users.FirstOrDefault(u => u.Username == LoggedInUser.Username && u.Password == LoggedInUser.Password);

            if (user != null)
            {
                Response.Cookies.Append(key: "UserLoginCookie", value: LoggedInUser.Username); // Установка cookie для аутентифицированного пользователя.
                Response.Redirect("/"); // Перенаправление на главную страницу.
            }
        }
    }

    // Класс для представления пользователя.
    public record class LoggedInUser(string Username, string Password) { }

    public class User
    { 
        public required string Username { get; set;}
        public required string Password { get; set;}
    }
}

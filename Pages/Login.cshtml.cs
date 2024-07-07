using Microsoft.AspNetCore.Mvc; // ������������ ���� ��� �������� ������������ � ��������
using Microsoft.AspNetCore.Mvc.RazorPages; // ������������ ���� ��� ������ � Razor Pages
using System.Text.Json; // ������������ ���� ��� ������ � JSON

namespace WebCompBot.Pages
{
    public class LoggingModel : PageModel
    {
        [BindProperty]
        public LoggedInUser LoggedInUser { get; set; } = new("", "");

        // ����� ��� ��������� POST-��������
        public void OnPost()
        {
            // ������ ������������� �� ����� logging.json.
            var users = JsonSerializer.Deserialize<List<User>>(System.IO.File.ReadAllText("uData/logging.json"));

            // �������� ������� ������������ � ���������� ������ � �������.
            var user = users.FirstOrDefault(u => u.Username == LoggedInUser.Username && u.Password == LoggedInUser.Password);

            if (user != null)
            {
                Response.Cookies.Append(key: "UserLoginCookie", value: LoggedInUser.Username); // ��������� cookie ��� �������������������� ������������.
                Response.Redirect("/"); // ��������������� �� ������� ��������.
            }
        }
    }

    // ����� ��� ������������� ������������.
    public record class LoggedInUser(string Username, string Password) { }

    public class User
    { 
        public required string Username { get; set;}
        public required string Password { get; set;}
    }
}

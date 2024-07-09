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
            var username = Request.Cookies["UserLoginCookie"]; // ��������� ����� ������������ �� cookie
            if (string.IsNullOrEmpty(username))
            {
                _logger.LogWarning("������� ������ ��� �����������.");
                Response.Redirect("/Login"); // ��������������� �� �������� �����������.
            }
            else
            {
                _logger.LogInformation("������������ {Username} ������� �� �������� ������.", username);
            }
        }

        // ����� ��� ��������� POST-��������
        public void OnPost()
        {
            var username = Request.Cookies["UserLoginCookie"];
            Response.Cookies.Delete(key: "UserLoginCookie"); // �������� cookie ��� �������������������� ������������.

            if (username != null)
            {
                _logger.LogInformation("������������ {Username} ������� �����.", username);
            }
            else
            {
                _logger.LogWarning("������� ������ ��� ������������ ������.");
            }

            Response.Redirect("/Login"); // ��������������� �� �������� �����������.
        }
    }
}

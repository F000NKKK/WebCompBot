using Microsoft.AspNetCore.Mvc; // ������������ ���� ��� �������� ������������ � ��������
using Microsoft.AspNetCore.Mvc.RazorPages; // ������������ ���� ��� ������ � Razor Pages
using System.Text.Json; // ������������ ���� ��� ������ � JSON

namespace WebCompBot.Pages
{
    public class LogoutModel : PageModel
    {
        public void OnGet()
        {
            var username = Request.Cookies["UserLoginCookie"]; // ��������� ����� ������������ �� cookie
            if (string.IsNullOrEmpty(username))
            {
                Response.Redirect("/Login"); // ��������������� �� �������� �����������.
            }
        }

        // ����� ��� ��������� POST-��������
        public void OnPost()
        {
            Response.Cookies.Delete(key: "UserLoginCookie"); // ��������� cookie ��� �������������������� ������������.

            Response.Redirect("/Login"); // ��������������� �� �������� �����������.
        }
    }
}

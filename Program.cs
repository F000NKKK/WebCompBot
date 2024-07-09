using NLog;
using NLog.Web;
using WebCompBot.RabbitMq;
using WebCompBot.SignalR;

namespace WebCompBot
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // ��������� NLog
            var logger = LogManager.Setup()
                .LoadConfigurationFromFile("nlog.config") // �������� ������������ �� �����
                .GetCurrentClassLogger();

            try
            {
                logger.Debug("������ ����������");

                var builder = WebApplication.CreateBuilder(args);

                // ���������� NLog � �������� ���������� �����������
                builder.Logging.ClearProviders();
                builder.Logging.AddNLogWeb();

                builder.Services.AddRazorPages();
                builder.Services.AddSingleton<RabbitMqBackgroundService>();
                builder.Services.AddHostedService<RabbitMqBackgroundService>(); // ����������� �������� ������� RabbitMQ
                builder.Services.AddSingleton<ISignalRService, SignalRService>();

                builder.Services.AddSignalR(); // ���������� SignalR
                builder.Services.AddSession();

                builder.Services.AddAuthentication("CookieAuth")
                    .AddCookie("CookieAuth", config =>
                    {
                        config.Cookie.Name = "UserLoginCookie";
                        config.LoginPath = "/Login";
                    });

                var app = builder.Build();

                if (!app.Environment.IsDevelopment())
                {
                    app.UseExceptionHandler("/Error");
                    app.UseHsts();
                }

                app.UseHttpsRedirection();
                app.UseStaticFiles();

                app.UseRouting();
                app.UseAuthentication();
                app.UseSession();

                app.MapGet("/UpdateTime", async context =>
                {
                    var dateTimeNow = DateTime.Now.ToString("HH:mm:ss");
                    await context.Response.WriteAsJsonAsync(new { time = dateTimeNow });
                });

                app.MapRazorPages();
                app.MapHub<ChatHub>("/chatHub"); // ����������� ChatHub

                app.Run();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "��������� ������ ��� ������� ����������");
                throw;
            }
            finally
            {
                LogManager.Shutdown();
            }
        }
    }
}

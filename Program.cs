using Microsoft.AspNetCore.SignalR;
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

                // ����������� IRabbitMqService ��� Singleton
                builder.Services.AddSingleton<IRabbitMqService, RabbitMqBackgroundService>();

                // ����������� RabbitMqBackgroundService ��� HostedService
                builder.Services.AddHostedService<RabbitMqBackgroundService>(provider =>
                {
                    var hubContext = provider.GetRequiredService<IHubContext<ChatHub>>();
                    var logger = provider.GetRequiredService<ILogger<RabbitMqBackgroundService>>();
                    var environment = provider.GetRequiredService<IWebHostEnvironment>();
                    var signalRService = provider.GetRequiredService<ISignalRService>();
                    return new RabbitMqBackgroundService(hubContext, logger, environment, signalRService);
                });

                // ����������� SignalRService ��� Singleton
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

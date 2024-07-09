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
            // Настройка NLog
            var logger = LogManager.Setup()
                .LoadConfigurationFromFile("nlog.config") // Загрузка конфигурации из файла
                .GetCurrentClassLogger();

            try
            {
                logger.Debug("Запуск приложения");

                var builder = WebApplication.CreateBuilder(args);

                // Добавление NLog в качестве провайдера логирования
                builder.Logging.ClearProviders();
                builder.Logging.AddNLogWeb();

                builder.Services.AddRazorPages();

                // Регистрация IRabbitMqService как Singleton
                builder.Services.AddSingleton<IRabbitMqService, RabbitMqBackgroundService>();

                // Регистрация RabbitMqBackgroundService как HostedService
                builder.Services.AddHostedService<RabbitMqBackgroundService>(provider =>
                {
                    var hubContext = provider.GetRequiredService<IHubContext<ChatHub>>();
                    var logger = provider.GetRequiredService<ILogger<RabbitMqBackgroundService>>();
                    var environment = provider.GetRequiredService<IWebHostEnvironment>();
                    var signalRService = provider.GetRequiredService<ISignalRService>();
                    return new RabbitMqBackgroundService(hubContext, logger, environment, signalRService);
                });

                // Регистрация SignalRService как Singleton
                builder.Services.AddSingleton<ISignalRService, SignalRService>();

                builder.Services.AddSignalR(); // Добавление SignalR
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
                app.MapHub<ChatHub>("/chatHub"); // Регистрация ChatHub

                app.Run();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Произошла ошибка при запуске приложения");
                throw;
            }
            finally
            {
                LogManager.Shutdown();
            }
        }
    }
}

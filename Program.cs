using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Json;
using Azure.Core;
using Microsoft.Extensions.FileProviders;
using static WebCompBot.Pages.IndexModel;

namespace WebCompBot
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddRazorPages();
            builder.Services.AddSingleton<RabbitMqBackgroundService>();
            builder.Services.AddHostedService<RabbitMqBackgroundService>();

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
            app.MapControllers();

            app.Run();
        }
    }
}

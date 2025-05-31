using StackExchange.Redis;

namespace Valuator;
using Microsoft.AspNetCore.Authentication.Cookies;


public class Program
{
 
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        

        var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
        var redis = ConnectionMultiplexer.Connect(redisConnectionString);
        builder.Services.AddSingleton<IConnectionMultiplexer>(redis);

        builder.Services.AddRazorPages();
        builder.Services.AddSignalR();
        
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/Registration"; // Перенаправление при неавторизованном доступе
                options.ExpireTimeSpan = TimeSpan.FromDays(2); // Срок действия куки
            });


        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
        }
        
        app.UseStaticFiles();
  
        app.UseRouting();

        app.UseAuthorization();

        app.MapRazorPages();

        app.Run();
 
        app.Run();
    }
}

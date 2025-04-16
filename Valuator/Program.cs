using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace Valuator;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

// Конфигурация подключений
        Console.WriteLine("DB_MAIN" + Environment.GetEnvironmentVariable("DB_MAIN"));
        Console.WriteLine("DB_EU" +  Environment.GetEnvironmentVariable("DB_EU"));
        Console.WriteLine("DB_RU" +  Environment.GetEnvironmentVariable("DB_RU"));
   

        builder.Services.AddKeyedSingleton<IConnectionMultiplexer>("MainRedis", 
            (_, _) => ConnectionMultiplexer.Connect(Environment.GetEnvironmentVariable("DB_MAIN")));

        builder.Services.AddKeyedSingleton<IConnectionMultiplexer>("RURedis", 
            (_, _) => ConnectionMultiplexer.Connect( Environment.GetEnvironmentVariable("DB_RU")));

        builder.Services.AddKeyedSingleton<IConnectionMultiplexer>("EURedis", 
            (_, _) => ConnectionMultiplexer.Connect( Environment.GetEnvironmentVariable("DB_EU")));

        builder.Services.AddKeyedSingleton<IConnectionMultiplexer>("ASIARedis", 
            (_, _) => ConnectionMultiplexer.Connect( Environment.GetEnvironmentVariable("DB_ASIA")));
        

        // Add services to the container.
        builder.Services.AddRazorPages();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
        }
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthorization();

        app.MapRazorPages();

        app.Run();
    }
}

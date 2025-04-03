using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using System.Runtime.Intrinsics.X86;
using System.Text;
using Valuator.Hubs;

namespace Valuator;

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

        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
        }
    
        // app.MapHub<ProcessingHub>("http://localhost:5005/processing-hub");   
        
        app.UseStaticFiles();
  
        app.UseRouting();

        app.UseAuthorization();

        app.MapRazorPages();

        app.Run();
 
        app.Run();
    }
}

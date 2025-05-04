using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

public class ProcessingHub : Hub
{
    public async Task Subscribe(string calculationId)
    {
        Console.WriteLine($"Subscribed to calculation: {calculationId}");
        await Groups.AddToGroupAsync(Context.ConnectionId, calculationId);
    }

    public async Task NotifyCompletion(string id, string message)
    {
        await Clients.Group(id).SendAsync("Receive", message);
    }
}

public class Program
{

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddSignalR();
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("SignalRCors", policy =>
            {
                policy.SetIsOriginAllowed(origin => 
                        new Uri(origin).Host == "localhost") // Разрешить все localhost-порты
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });
       
        var app = builder.Build();
       
        app.UseCors("SignalRCors");
        app.MapHub<ProcessingHub>("/processing-hub");

        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthorization();

        app.Run();
    }
}

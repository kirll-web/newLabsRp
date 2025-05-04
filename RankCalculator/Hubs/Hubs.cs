//
//
// using Microsoft.AspNetCore.SignalR;
// using StackExchange.Redis;
//
// namespace Valuator.Hubs
// {
//     public class ProcessingHub : Hub
//     {
//         public async Task SubscribeToResults(string id)
//         {
//             await Groups.AddToGroupAsync(Context.ConnectionId, id);
//         }
//         
//         public void ConfigureServices(IServiceCollection services)
//         {
//             services.AddSignalR();
//             services.AddSingleton<IDatabase>(_ => 
//                 ConnectionMultiplexer.Connect("localhost").GetDatabase());
//         }
//
//         public void Configure(IApplicationBuilder app)
//         {
//             app.UseEndpoints(endpoints =>
//             {
//                 endpoints.MapHub<ProcessingHub>("/processingHub");
//             });
//         }
//     }
// }
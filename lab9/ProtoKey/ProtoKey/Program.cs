using System.Text.RegularExpressions;
using System.Threading.Channels;
using ProtoKey;
using ProtoKey.Core;
using ProtoKey.Services;


var builder = WebApplication.CreateBuilder(args);
var keyRegex = new Regex("^[a-zA-Z0-9_.-]{1,1000}$", RegexOptions.Compiled);



//builder
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


//add channels
builder.Services.AddSingleton<ICommandChannel, CommandChannelWrapper>();
builder.Services.AddSingleton<IPersistChannel, PersistChannelWrapper>();

//add services
builder.Services.AddHostedService<KeyValueWorker>();
builder.Services.AddHostedService<PersistenceWorker>();


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();

//Queries
app.MapGet("/", () =>
{
    return Results.Ok("Hello World!");
});

// POST
app.MapPost("/set", async (SetRequest req, ICommandChannel commandChannel, IPersistChannel persistChannel) =>
{
    if (string.IsNullOrWhiteSpace(req.Key) || !keyRegex.IsMatch(req.Key))
        return Results.BadRequest("Invalid key");

    if (req.Value is null)
        return Results.BadRequest("Value is required");

    var command = new Command
    {
        Type = CommandType.Set,
        Key = req.Key,
        Value = req.Value
    };
    
    var persistCommand = new Command
    {
        Type = CommandType.Set,
        Key = req.Key,
        Value = req.Value
    };
    await commandChannel.Channel.Writer.WriteAsync(command);
    await persistChannel.Channel.Writer.WriteAsync(persistCommand);
    await command.Completion.Task;

    return Results.Ok();
});

// GET
app.MapGet("/get", async (string? key, ICommandChannel commandChannel, IPersistChannel persistChannel) =>
{
    if (string.IsNullOrWhiteSpace(key) || !keyRegex.IsMatch(key))
        return Results.BadRequest("Invalid key");

    var command = new Command
    {
        Type = CommandType.Get,
        Key = key
    };
    
    var persistCommand = new Command
    {
        Type = CommandType.Get,
        Key = key
    };

    await commandChannel.Channel.Writer.WriteAsync(command);
    await persistChannel.Channel.Writer.WriteAsync(persistCommand);
    var result = await command.Completion.Task;

    return Results.Ok(result);
});

// GET
app.MapGet("/keys", async (string? prefix, ICommandChannel commandChannel, IPersistChannel persistChannel) =>
{
    prefix ??= "";

    if (prefix != "" && !keyRegex.IsMatch(prefix))
        return Results.BadRequest("Invalid prefix");

    var command = new Command
    {
        Type = CommandType.Keys,
        Prefix = prefix
    };
    
    var persistCommand = new Command
    {
        Type = CommandType.Keys,
        Prefix = prefix
    };

    await commandChannel.Channel.Writer.WriteAsync(command);
    await persistChannel.Channel.Writer.WriteAsync(persistCommand);
    var result = await command.Completion.Task;

    return Results.Ok(result);
});



//Run
app.Run();

using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RabbitMQ.Client;
using StackExchange.Redis;

namespace Valuator.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IDatabase _mainDb;
    private readonly IDatabase _ruDb;
    private readonly IDatabase _euDb;
    private readonly IDatabase _asiaDb;

    private readonly ConnectionFactory _factory;
    private const string QueueName = "valuator.processing.rank";
    private const string QueueEvents = "valuator.events";
    private const string SimilirityEvent = "SimilarityCalculated";
    private const string exchangeName = "events";

    public IndexModel(ILogger<IndexModel> logger, [FromKeyedServices("MainRedis")] IConnectionMultiplexer mainRedis,
        [FromKeyedServices("RURedis")] IConnectionMultiplexer ruRedis,
        [FromKeyedServices("EURedis")] IConnectionMultiplexer euRedis,
        [FromKeyedServices("ASIARedis")] IConnectionMultiplexer asiaRedis)
    {
        _logger = logger;


        _mainDb = mainRedis.GetDatabase();
        _ruDb = ruRedis.GetDatabase();
        _euDb = euRedis.GetDatabase();
        _asiaDb = asiaRedis.GetDatabase();
    }


    public async Task<IActionResult> OnPostAsync(string text, string region)
    {
        string id = Guid.NewGuid().ToString();
        double similarity = CalculateSimilarityAsync(text, region);

        await _mainDb.StringSetAsync($"{id}", region);

        getDb(region).StringSet($"SIMILARITY-{id}", similarity.ToString());
        getDb(region).StringSet($"TEXT-{id}", text != null ? text : "");
        await SendSimilirityEvent(id, region, similarity);
        _logger.LogInformation($"LOOKUP: {id},  {region}*");
        
        await SendMessageToQueue($"{id}", region);

        return Redirect($"summary?id={id}");
    }

    private async Task SendMessageToQueue(string id, string region)
    {
        CancellationTokenSource cts = new CancellationTokenSource();
        Task produceTask = ProduceAsync(cts.Token, id, region);

        await produceTask;
        cts.Cancel();
    }


    private async Task ProduceAsync(CancellationToken ct, string id, string region)
    {
        ConnectionFactory factory = new ConnectionFactory
        {
            HostName = "localhost"
        };
        await using IConnection connection = await factory.CreateConnectionAsync(ct);
        await using IChannel channel = await connection.CreateChannelAsync(null, ct);

        await DeclareTopologyAsync(channel, ct);

        var messageObject = new
        {
            Id = id
        };

        var json = JsonSerializer.Serialize(messageObject);
        var body = Encoding.UTF8.GetBytes(json);

        await channel.BasicPublishAsync(
            exchange: "",
            routingKey: QueueName,
            mandatory: false,
            body: body
        );

        await connection.CloseAsync(ct);
    }

    private async Task SendSimilirityEvent(string id, string region, double similarity)
    {
        CancellationTokenSource cts = new CancellationTokenSource();
        Task produceTask = ProduceSimilirityEvent(cts.Token, id, region, similarity);

        await produceTask;
        cts.Cancel();
    }

    private IDatabase getDb(string region)
    {
        if (region == "RU")
        {
            return _ruDb;
        }
        else if (region == "EU")
        {
            return _euDb;
        }
        else
        {
            return _asiaDb;
        }
    }

    private async Task ProduceSimilirityEvent(CancellationToken ct, string id, string region, double similarity)
    {
        ConnectionFactory factory = new ConnectionFactory
        {
            HostName = "localhost"
        };
        await using IConnection connection = await factory.CreateConnectionAsync(ct);
        await using IChannel channel = await connection.CreateChannelAsync(null, ct);

        await DeclareTopologyAsyncForSimilirityEvents(channel, ct);

        var messageObject = new
        {
            Id = id,
            Similarity = similarity,
            Region = region
        };

        var json = JsonSerializer.Serialize(messageObject);
        var body = Encoding.UTF8.GetBytes(json);

        await channel.BasicPublishAsync(
            exchange: "",
            routingKey: QueueName,
            mandatory: false,
            body: body
        );

        await channel.BasicPublishAsync(
            exchange: exchangeName,
            routingKey: SimilirityEvent,
            mandatory: false,
            body: body
        );
        await connection.CloseAsync(ct);
    }

    private static async Task DeclareTopologyAsync(IChannel channel, CancellationToken ct)
    {
        await channel.ExchangeDeclareAsync(
            exchange: QueueName,
            type: ExchangeType.Direct,
            cancellationToken: ct
        );
        await channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: ct
        );
        await channel.QueueBindAsync(
            queue: QueueName,
            exchange: QueueName,
            routingKey: "",
            cancellationToken: ct);
    }

    private static async Task DeclareTopologyAsyncForSimilirityEvents(IChannel channel, CancellationToken ct)
    {
        await channel.ExchangeDeclareAsync(
            exchange: exchangeName,
            type: ExchangeType.Direct,
            durable: true,
            cancellationToken: ct
        );
        await channel.QueueDeclareAsync(
            queue: QueueEvents,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: ct
        );
        await channel.QueueBindAsync(
            queue: QueueEvents,
            exchange: exchangeName,
            routingKey: SimilirityEvent,
            cancellationToken: ct);
    }

    private double CalculateSimilarityAsync(string currentText, string region)
    {
        var keys = getDb(region).Multiplexer.GetServer(getDb(region).Multiplexer.GetEndPoints().First())
            .Keys(pattern: "TEXT-*");

        foreach (var key in keys)
        {
            try
            {
                var storedText = getDb(region).StringGet(key);
                if (storedText == currentText)
                {
                    return 1;
                }
            }
            catch (Exception ex)
            {
                continue;
            }
        }

        return 0;
    }

    private double CalculateRank(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;

        int nonAlphabeticCount = 0;
        foreach (char c in text)
        {
            if (!IsAlphabetic(c))
                nonAlphabeticCount++;
        }

        return (double)nonAlphabeticCount / text.Length;
    }

    private bool IsAlphabetic(char c)
    {
        return char.IsLetter(c) &&
               (c <= 0x007F ||
                c >= 0x0410 && c <= 0x044F);
    }
}

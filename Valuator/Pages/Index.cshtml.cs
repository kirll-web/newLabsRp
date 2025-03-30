using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StackExchange.Redis;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace Valuator.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IDatabase _redisDb;
    private readonly ConnectionFactory _factory;
    private const string QueueName = "valuator.processing.rank";

    public IndexModel(ILogger<IndexModel> logger, IConnectionMultiplexer redis)
    {
        _logger = logger;
        _redisDb = redis.GetDatabase();
        _factory = new ConnectionFactory { HostName = "localhost" };
    }

    public async Task<IActionResult> OnPostAsync(string text)
    {
        string id = Guid.NewGuid().ToString();

        await _redisDb.StringSetAsync($"TEXT-{id}", text);
        _logger.LogInformation($"OnPostAsync: {id}");

        await SendMessageToQueue($"{id}");


        double similarity = CalculateSimilarityAsync(text);
        _redisDb.StringSet($"SIMILARITY-{id}", similarity.ToString());
        _redisDb.StringSet($"TEXT-{id}", text != null ? text : "");

        return Redirect($"summary?id={id}");
    }

    private async Task SendMessageToQueue(string id)
    {
        CancellationTokenSource cts = new CancellationTokenSource();
        Task produceTask = ProduceAsync(cts.Token, id);

        await produceTask; // Дожидаемся завершения ProduceAsync
        cts.Cancel();
        _logger.LogInformation($"Sent message: {id}");
    }


    private async Task ProduceAsync(CancellationToken ct, string id)
    {
        // Установка соединения с RabbitMQ по адресу localhost:5672
        ConnectionFactory factory = new ConnectionFactory
        {
            HostName = "localhost"
        };
        await using IConnection connection = await factory.CreateConnectionAsync(ct);
        await using IChannel channel = await connection.CreateChannelAsync(null, ct);

        await DeclareTopologyAsync(channel, ct);

        // Отправка сообщения ежесекундно в цикле.
        string message = $"{id}";
        byte[] body = Encoding.UTF8.GetBytes(message);

        await channel.BasicPublishAsync(
            exchange: "",
            routingKey: QueueName,
            mandatory: false,
            body: body
        );

        _logger.LogInformation($"ProduceAsync Sent message: {id}");
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

    private double CalculateSimilarityAsync(string currentText)
    {
        var keys = _redisDb.Multiplexer.GetServer(_redisDb.Multiplexer.GetEndPoints().First()).Keys(pattern: "TEXT-*");

        foreach (var key in keys)
        {
           try
            {
                var storedText = _redisDb.StringGet(key);
                if (storedText == currentText)
                {
                    return 1;
                }
            } catch(Exception ex)
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
        // Проверка на русские и латинские буквы
        return char.IsLetter(c) &&
               (c <= 0x007F ||  // ASCII
                c >= 0x0410 && c <= 0x044F); // Русские буквы
    }

}
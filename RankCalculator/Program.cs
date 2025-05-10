using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text;
using Microsoft.AspNetCore.SignalR.Client;
using StackExchange.Redis;

namespace RankCalculator;

public class MainProgram
{
    public static async Task Main(string[] args)
    {
        var redis = await ConnectionMultiplexer.ConnectAsync("localhost");
        var redisDb = redis.GetDatabase();
        var rankCalculator = new RankCalculator(redisDb);
        rankCalculator.Start(args);
    }
}

public class RankCalculator(IDatabase redis)
{
    private const string QueueName = "valuator.processing.rank";
    private const string QueueEvents = "valuator.events";
    private const string exchangeName = "events";
    private const string hubUrl = "http://localhost:5005/processing-hub";
    private const string RankCalculatedEvent = "RankCalculated";

    public async Task Start(string[] args)
    {
        Console.WriteLine("RankCalculator started");

        var factory = new ConnectionFactory
        {
            HostName = "localhost"
        };

        var connection = await factory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();

        await DeclareTopologyAsync(channel);
        // await RunRankCalculator(channel);

        Console.WriteLine("Press Enter to exit");
        Console.ReadLine();
    }

    private async Task RunRankCalculator(IChannel channel)
    {
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            var t = Task.Run(async delegate
            {
                TimeSpan interval = TimeSpan.FromSeconds(new Random().Next(3, 5)); //fixme mock
                Console.WriteLine($"Waiting {interval}");

                await Task.Delay(interval);
                return 42;
            });
            t.Wait();
            Console.WriteLine("Consuming");
            string id = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
            Console.WriteLine($"Consuming: {id} from queue {QueueName}");


            double rank = CalculateRank(id);
            await redis.StringSetAsync($"RANK-{id}", rank.ToString());

            await SendRankCalculatedEvent(id, rank);

            Console.WriteLine($"Computed rank: {rank} for id: {id}");
            var hubConnection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .Build();

            await hubConnection.StartAsync();
            await hubConnection.InvokeAsync("NotifyCompletion", id, $"{rank}");
            await channel.BasicAckAsync(eventArgs.DeliveryTag, false);
        };

        await channel.BasicConsumeAsync(
            queue: QueueName,
            autoAck: false,
            consumer: consumer
        );
    }

    private static async Task DeclareTopologyAsync(IChannel channel)
    {
        await channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false
        );
    }

    internal double CalculateRank(string id)
    {
        string text = redis.StringGet($"TEXT-{id}");
        if (string.IsNullOrEmpty(text)) return 0;

        var nonAlphabeticCount = text.Count(c => !IsAlphabetic(c));
        return (double)nonAlphabeticCount / text.Length;
    }

    private async Task SendRankCalculatedEvent(string id, double rank)
    {
        CancellationTokenSource cts = new CancellationTokenSource();
        Task produceTask = ProduceRankCalculatedEvent(cts.Token, id, rank);

        await produceTask;
        cts.Cancel();
    }

    private async Task ProduceRankCalculatedEvent(CancellationToken ct, string id, double rank)
    {
        ConnectionFactory factory = new ConnectionFactory
        {
            HostName = "localhost"
        };
        await using IConnection connection = await factory.CreateConnectionAsync(ct);
        await using IChannel channel = await connection.CreateChannelAsync(null, ct);

        await DeclareTopologyAsyncForRankCalculated(channel, ct);


        string message = $"{id}|{rank}";
        byte[] body = Encoding.UTF8.GetBytes(message);

        await channel.BasicPublishAsync(
            exchange: exchangeName,
            routingKey: RankCalculatedEvent,
            mandatory: false,
            body: body
        );

        await connection.CloseAsync(ct);
    }

    private static async Task DeclareTopologyAsyncForRankCalculated(IChannel channel, CancellationToken ct)
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
            routingKey: RankCalculatedEvent,
            cancellationToken: ct);
    }

    internal static bool IsAlphabetic(char c)
    {
        return char.IsLetter(c) &&
               (c <= 0x007F || c >= 0x0410 && c <= 0x044F || c == 'ё');
    }
}

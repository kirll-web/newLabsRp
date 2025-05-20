using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text;
using Microsoft.AspNetCore.SignalR.Client;
using StackExchange.Redis;

namespace RankCalculator;

public class RankCalculator
{
    private const string QueueName = "valuator.processing.rank";
    private const string QueueEvents = "valuator.events";
    private const string exchangeName = "events";
    private  string hubUrl = Environment.GetEnvironmentVariable("ConnectionStrings__Hub") ?? "http://localhost:5005";

    
    private const string RankCalculatedEvent = "RankCalculated";
    private static IDatabase redis;
    private readonly ConnectionFactory _factory;

    public RankCalculator()
    {
        Console.WriteLine("HubUrl hubUrl");
        var rabbitMqUrl = Environment.GetEnvironmentVariable("RabbitMQ__Url") ?? "localhost";
        Console.WriteLine($" rabbitMqUrl {rabbitMqUrl}");
        Console.WriteLine($" rabbitMqUrl");

        _factory = rabbitMqUrl == "localhost" ? new ConnectionFactory {
            HostName = rabbitMqUrl
        } : new ConnectionFactory{
            HostName = rabbitMqUrl.Split(':')[0],
            Port = int.Parse(rabbitMqUrl.Split(':').Length > 1 ? rabbitMqUrl.Split(':')[1] : "5672"),
            UserName = "guest",
            Password = "guest"
        };
        
        Console.WriteLine($" rabbitMqUrl {_factory.HostName} {_factory.Port} {_factory.UserName} {_factory.Password}");
    }
    
    public static void Main(string[] args)
    {
        Console.WriteLine("Consumer started");
        string redisConnection = Environment.GetEnvironmentVariable("ConnectionStrings__Redis") ?? "localhost:6379";
        Console.WriteLine($"Consumer started redisConnection redisConnection {redisConnection}");
        var rediss = ConnectionMultiplexer.Connect(redisConnection);
        var redisDb = rediss.GetDatabase();
        var calculator = new RankCalculator(); // Создаем экземпляр
        redis = redisDb;
        calculator.Start(redisDb).GetAwaiter().GetResult();
    }
    
    public async Task Start(IDatabase sredis)
    {
        redis = sredis;
        Console.WriteLine("RankCalculator started");
        
        var connection = await _factory.CreateConnectionAsync();
        Console.WriteLine("1");
        var channel = await connection.CreateChannelAsync();
        Console.WriteLine("2");
        await DeclareTopologyAsync(channel);
        Console.WriteLine("3");
        await RunRankCalculator(channel);
        Console.WriteLine("4");

        Console.WriteLine("Enter X to exit");
        while (true)
        {
            var line = Console.ReadLine();
            if (line == "x") break;
        }
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
            var url = $"{hubUrl}/processing-hub";
            Console.WriteLine(url);

            var hubConnection = new HubConnectionBuilder()
                .WithUrl(url)
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
        await using IConnection connection = await _factory.CreateConnectionAsync(ct);
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

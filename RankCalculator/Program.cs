using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using StackExchange.Redis;

public class MessageDto
{
    public string Id { get; set; }
}


namespace RankCalculator
{
    public class Consumer
    {
        private const string QueueName = "valuator.processing.rank";
        private const string QueueEvents = "valuator.events";
        private const string exchangeName = "events";

        private const string RankCalculatedEvent = "RankCalculated";
        private static IDatabase _redisMainDb;
        private static Dictionary<string, IDatabase> _regionalDbs; 

        public static async Task Main(string[] args)
        {
            Console.WriteLine("Consumer started");

            var factory = new ConnectionFactory
            {
                HostName = "localhost"
            };

            
            var mainConnection = ConnectionMultiplexer.Connect(
                Environment.GetEnvironmentVariable("DB_MAIN"));
            _redisMainDb = mainConnection.GetDatabase();
        
            _regionalDbs = new Dictionary<string, IDatabase>
            {
                ["RU"] = ConnectionMultiplexer.Connect(
                    Environment.GetEnvironmentVariable("DB_RU")).GetDatabase(),
                ["EU"] = ConnectionMultiplexer.Connect(
                    Environment.GetEnvironmentVariable("DB_EU")).GetDatabase(),
                ["ASIA"] = ConnectionMultiplexer.Connect(
                    Environment.GetEnvironmentVariable("DB_ASIA")).GetDatabase()
            };

            var connection = await factory.CreateConnectionAsync();
            var channel = await connection.CreateChannelAsync();

            await DeclareTopologyAsync(channel);
            await RunConsumer(channel);

            Console.WriteLine("Press Enter to exit");
            Console.ReadLine();
        }

        private static async Task RunConsumer(IChannel channel)
        {
            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (_, eventArgs) =>
            {
                var body = eventArgs.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);
    
                var message = JsonSerializer.Deserialize<MessageDto>(json);
                string region = _redisMainDb.StringGet($"{message.Id}");
                double rank = CalculateRank(message.Id, region);
                await _regionalDbs[region].StringSetAsync($"RANK-{message.Id}", rank.ToString());
                await SendRankCalculatedEvent(message.Id, rank, region);
                Console.WriteLine($"LOOKUP: {message.Id},  {region}*");
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

        private static double CalculateRank(string id, string region)
        {
            string text = _regionalDbs[region].StringGet($"TEXT-{id}");
            if (string.IsNullOrEmpty(text)) return 0;

            int nonAlphabeticCount = 0;
            foreach (char c in text)
            {
                if (!IsAlphabetic(c))
                    nonAlphabeticCount++;
            }
            return (double)nonAlphabeticCount / text.Length;
        }
        
        private static async Task SendRankCalculatedEvent(string id, double rank, string region)
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            Task produceTask = ProduceRankCalculatedEvent(cts.Token, id, rank, region);

            await produceTask; 
            cts.Cancel();
        }

        private static async Task ProduceRankCalculatedEvent(CancellationToken ct, string id, double rank, string region)
        {
            ConnectionFactory factory = new ConnectionFactory
            {
                HostName = "localhost"
            };
            await using IConnection connection = await factory.CreateConnectionAsync(ct);
            await using IChannel channel = await connection.CreateChannelAsync(null, ct);

            await DeclareTopologyAsyncForRankCalculated(channel, ct);
            
            var messageObject = new 
            {
                Id = id,
                Rank = rank,
                Region = region
            };

            var json = JsonSerializer.Serialize(messageObject);
            var body = Encoding.UTF8.GetBytes(json);

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

        private static bool IsAlphabetic(char c)
        {
            return char.IsLetter(c) &&
                   (c <= 0x007F || c >= 0x0410 && c <= 0x044F);
        }
    }
}

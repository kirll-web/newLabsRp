using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text;
using StackExchange.Redis;

namespace RankCalculator
{
    public class Consumer
    {
        private const string QueueName = "valuator.processing.rank";
        private static IDatabase _redisDb;

        public static async Task Main(string[] args)
        {
            Console.WriteLine("Consumer started");

            var factory = new ConnectionFactory
            {
                HostName = "localhost"
            };

            var redis = await ConnectionMultiplexer.ConnectAsync("localhost");
            _redisDb = redis.GetDatabase();

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
                Console.WriteLine("Consuming");
                string id = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
                Console.WriteLine($"Consuming: {id} from queue {QueueName}");


                double rank = CalculateRank(id);
                await _redisDb.StringSetAsync($"RANK-{id}", rank.ToString());

                Console.WriteLine($"Computed rank: {rank} for id: {id}");

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

        private static double CalculateRank(string id)
        {
            Console.WriteLine($"CalculateRank id {id}");
            string text = _redisDb.StringGet($"TEXT-{id}");
            Console.WriteLine($"CalculateRank text {text}");
            if (string.IsNullOrEmpty(text)) return 0;

            int nonAlphabeticCount = 0;
            foreach (char c in text)
            {
                if (!IsAlphabetic(c))
                    nonAlphabeticCount++;
            }
            return (double)nonAlphabeticCount / text.Length;
        }

        private static bool IsAlphabetic(char c)
        {
            return char.IsLetter(c) &&
                   (c <= 0x007F || c >= 0x0410 && c <= 0x044F);
        }
    }
}
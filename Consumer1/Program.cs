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

            ConnectionFactory factory = new ConnectionFactory
            {
                HostName = "localhost",
            };
            ConnectionMultiplexer redis = await ConnectionMultiplexer.ConnectAsync("localhost");
            _redisDb = redis.GetDatabase();
            await using IConnection connection = await factory.CreateConnectionAsync();
            await using IChannel channel = await connection.CreateChannelAsync();

            await DeclareTopologyAsync(channel);
            string consumerTag = await RunConsumer(channel);

            Console.WriteLine("Press Enter to exit");
            Console.ReadLine();

            await channel.BasicCancelAsync(consumerTag);

            Console.WriteLine("done");
        }

        private static async Task<string> RunConsumer(IChannel channel)
        {
            AsyncEventingBasicConsumer consumer = new(channel);
            consumer.ReceivedAsync += (_, eventArgs) => ConsumeAsync(channel, eventArgs);
            return await channel.BasicConsumeAsync(
                queue: QueueName,
                autoAck: false,
                consumer: consumer
            );
        }

        private static async Task ConsumeAsync(IChannel channel, BasicDeliverEventArgs eventArgs)
        {
            Console.WriteLine("Consuming");
            string message = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
            Console.WriteLine($"1 Consuming: {message} from subject {eventArgs.Exchange}");

            string[] parts = message.Split('|');
            if (parts.Length != 2) return;
            Console.WriteLine($"ConsumeAsync: 2 ");
            string id = parts[0];
            string text = parts[1];
            Console.WriteLine($"ConsumeAsync: 3 {text}");
            double rank = CalculateRank(text);
            Console.WriteLine($"ConsumeAsync: 4 {text}");
            Console.WriteLine($"ConsumeAsync: 5 {text} {rank}");
            await _redisDb.StringSetAsync($"RANK-{id}", rank.ToString());

            string rankValue = _redisDb.StringGet($"RANK-{id}");
            Console.WriteLine($"6 ConsumeAsync: RANK-{text} { rankValue}");

            Console.WriteLine($"7 Computed rank: {rank} for id: {id}");

            await channel.BasicAckAsync(eventArgs.DeliveryTag, false);

        }


        /// <summary>
        ///  Определяет топологию: queue -> consumer.
        /// </summary>
        private static async Task DeclareTopologyAsync(IChannel channel)
        {
            await channel.QueueDeclareAsync(
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false
            );
        }

        private static double CalculateRank(string text)
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

        private static bool IsAlphabetic(char c)
        {
            // Проверка на русские и латинские буквы
            return char.IsLetter(c) &&
                   (c <= 0x007F ||  // ASCII
                    c >= 0x0410 && c <= 0x044F); // Русские буквы
        }
    }

}
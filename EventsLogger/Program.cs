using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text;
using StackExchange.Redis;

namespace EventsLogger
{
    public class Consumer
    {
        private const string QueueEvents = "valuator.events";
        private const string RankCalculatedEvent = "RankCalculated";
        private const string SimilarityCalculated = "SimilarityCalculated";
        private const string exchangeName = "events";

        
        public static async Task Main(string[] args)
        {
            Console.WriteLine("Consumer started");

            var factory = new ConnectionFactory
            {
                HostName = "localhost"
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
                string[] parts = Encoding.UTF8.GetString(eventArgs.Body.ToArray()).Split(new[] { '|' }, 2) ?? new string[0];
                
                string id = parts[0];
                string value = parts[1];
                Console.WriteLine($"Received event: {eventArgs.RoutingKey}, id: {id}, value: {value}");
                await channel.BasicAckAsync(eventArgs.DeliveryTag, false);
            };

            await channel.BasicConsumeAsync(
                queue: QueueEvents,
                autoAck: false, 
                consumer: consumer
            );
        }

        private static async Task DeclareTopologyAsync(IChannel channel)
        {

            // Объявляем обменник для событий
            await channel.ExchangeDeclareAsync(
                exchange: exchangeName,
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false
            );

            // Объявляем очередь
            await channel.QueueDeclareAsync(
                queue: QueueEvents,
                durable: true,
                exclusive: false,
                autoDelete: false
            );

            // Привязываем очередь к обменнику с двумя routing keys
            await channel.QueueBindAsync(
                queue: QueueEvents,
                exchange: exchangeName,
                routingKey: RankCalculatedEvent
            );

            await channel.QueueBindAsync(
                queue: QueueEvents,
                routingKey: SimilarityCalculated,
                exchange: exchangeName
            );
        }
    }
}

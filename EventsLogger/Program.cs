using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using StackExchange.Redis;

public class MessageRank
{
    public string Id { get; set; }
    public double  Rank { get; set; }
    public string Region { get; set; }
}

public class MessageSimilarity
{
    public string Id { get; set; }
    public double  Similarity { get; set; }
    public string Region { get; set; }
}

namespace EventsLogger
{
    public class Consumer
    {
        private const string QueueEvents = "valuator.events";
        private const string RoutingRankCalculated = "RankCalculated";
        private const string RoutingSimilarityCalculated = "SimilarityCalculated";
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
                Console.WriteLine($"DEGUG EVENT");
                var body = eventArgs.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);
                Console.WriteLine($"1 DEGUG EVENT {eventArgs.RoutingKey}");
                if (eventArgs.RoutingKey == RoutingRankCalculated)
                {
                    Console.WriteLine($" 2 DEGUG EVENT {eventArgs.RoutingKey} {json}");
                    try
                    {
                        var message = JsonSerializer.Deserialize<MessageRank>(json);
                        Console.WriteLine($"Received event: {eventArgs.RoutingKey}, id: {message.Id}, value: {message.Rank}");
                        Console.WriteLine($"LOOKUP: {message.Id},  {message.Region}*");
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(exception);
                    }
                   
                } else if (eventArgs.RoutingKey == RoutingSimilarityCalculated)
                {
                    Console.WriteLine($" 2 DEGUG EVENT {eventArgs.RoutingKey} {JsonSerializer.Deserialize<MessageSimilarity>(json)}");
                    try
                    {
                        var message = JsonSerializer.Deserialize<MessageSimilarity>(json);
                        Console.WriteLine($"Received event: {eventArgs.RoutingKey}, id: {message.Id}, value: {message.Similarity}");
                        Console.WriteLine($"LOOKUP: {message.Id},  {message.Region}*");
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(exception);
                    }
                }
                else
                {
                    Console.WriteLine($"Unknown routing key");
                }
                
                await channel.BasicAckAsync(eventArgs.DeliveryTag, false);
            };

            await channel.BasicConsumeAsync(
                queue: QueueEvents,
                autoAck: true, 
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
                routingKey: RoutingRankCalculated
            );

            await channel.QueueBindAsync(
                queue: QueueEvents,
                routingKey: RoutingSimilarityCalculated,
                exchange: exchangeName
            );
        }
    }
}

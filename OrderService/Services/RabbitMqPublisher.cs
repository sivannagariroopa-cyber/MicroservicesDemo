using OrderService.Events;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace OrderService.Services
{
    public class RabbitMqPublisher
    {
        private readonly IConfiguration _configuration;

        public RabbitMqPublisher(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task PublishAsync(OrderCreatedEvent orderEvent)
        {
            var host = _configuration["RabbitMQ:Host"];
            var port = int.Parse(_configuration["RabbitMQ:Port"]!);
            var username = _configuration["RabbitMQ:Username"];
            var password = _configuration["RabbitMQ:Password"];

            var exchange = _configuration["RabbitMQ:Exchange"];
            var queue = _configuration["RabbitMQ:Queue"];
            var routingKey = _configuration["RabbitMQ:RoutingKey"];

            var factory = new ConnectionFactory
            {
                HostName = host,
                Port = port,
                UserName = username,
                Password = password
            };

            await using var connection =
                await factory.CreateConnectionAsync();

            await using var channel =
                await connection.CreateChannelAsync();

            Console.WriteLine("RabbitMQ connected");

            // 1. Declare exchange
            await channel.ExchangeDeclareAsync(
                exchange: exchange,
                type: ExchangeType.Direct,
                durable: true);

            Console.WriteLine($"Exchange created: {exchange}");

            // 2. Declare queue
            await channel.QueueDeclareAsync(
                queue: queue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            Console.WriteLine($"Queue created: {queue}");

            // 3. Bind queue to exchange
            await channel.QueueBindAsync(
                queue: queue,
                exchange: exchange,
                routingKey: routingKey);

            Console.WriteLine(
                $"Queue bound: {queue} -> {exchange} -> {routingKey}");

            // 4. Serialize event
            var json = JsonSerializer.Serialize(orderEvent);

            var body = Encoding.UTF8.GetBytes(json);

            Console.WriteLine($"Publishing: {json}");

            // 5. Publish
            await channel.BasicPublishAsync(
                exchange: exchange,
                routingKey: routingKey,
                body: body);

            Console.WriteLine("Message published successfully");
        }
    }
}

using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using PaymentService.Events;
namespace PaymentService.Services
{
    public class RabbitMqConsumer : BackgroundService
    {
        private readonly IConfiguration _configuration;

        private IConnection? _connection;
        private IChannel? _channel;

        public RabbitMqConsumer(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            Console.WriteLine("=== RabbitMqConsumer STARTED ===");
            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMQ:Host"],
                Port = int.Parse(
                    _configuration["RabbitMQ:Port"]!),
                UserName = _configuration["RabbitMQ:Username"],
                Password = _configuration["RabbitMQ:Password"]
            };

            _connection =
                await factory.CreateConnectionAsync();

            _channel =
                await _connection.CreateChannelAsync();

            var exchange =
                _configuration["RabbitMQ:Exchange"];

            var queue =
                _configuration["RabbitMQ:Queue"];

            var routingKey =
                _configuration["RabbitMQ:RoutingKey"];

            await _channel.ExchangeDeclareAsync(
                exchange,
                ExchangeType.Direct,
                durable: true);

            await _channel.QueueDeclareAsync(
                queue,
                durable: true,
                exclusive: false,
                autoDelete: false);

            await _channel.QueueBindAsync(
                queue,
                exchange,
                routingKey);

            var consumer =
                new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (sender, args) =>
            {
                try
                {
                    var json =
                        Encoding.UTF8.GetString(
                            args.Body.ToArray());

                    var orderEvent =
                        JsonSerializer.Deserialize<OrderCreatedEvent>(
                            json);

                    if (orderEvent == null)
                        return;

                    Console.WriteLine(
                        $"Received Order: {orderEvent.OrderId}");

                    Console.WriteLine(
                        $"Product: {orderEvent.ProductId}");

                    Console.WriteLine(
                        $"Quantity: {orderEvent.Quantity}");

                    Console.WriteLine(
                        $"Amount: {orderEvent.Amount}");

                    // Process payment here

                    await _channel.BasicAckAsync(
                        args.DeliveryTag,
                        multiple: false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);

                    await _channel.BasicNackAsync(
                        args.DeliveryTag,
                        multiple: false,
                        requeue: true);
                }
            };

            await _channel.BasicConsumeAsync(
                queue,
                autoAck: false,
                consumer: consumer);

            await Task.Delay(
                Timeout.Infinite,
                stoppingToken);
        }
    }
}

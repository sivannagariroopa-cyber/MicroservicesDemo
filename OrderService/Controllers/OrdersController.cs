using Azure.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Events;
using OrderService.Interfaces;
using OrderService.Models;
using OrderService.Services;
using OrderService.Services.Clients;
using RabbitMQ.Client;
namespace OrderService.Controllers
{
    [ApiController]
    [Route("api/orders")]
    public class OrdersController : ControllerBase
    {
        private readonly OrderDbContext _context;
        private readonly OrderManager _orderManager;
        private readonly IProductClient _productClient;
        private readonly RabbitMqPublisher _rabbitMqPublisher;
        public OrdersController(OrderDbContext context, OrderManager orderManager, IProductClient productClient,
        RabbitMqPublisher rabbitMqPublisher)
        {
            _context = context;
            _orderManager = orderManager;
            _productClient = productClient;
            _rabbitMqPublisher = rabbitMqPublisher;
        }
        //public async Task<IActionResult> CreateOrder(Order order)
        //{
        //    order.CreatedDate = DateTime.UtcNow;
        //    order.Status = "Created";

        //    _context.Orders.Add(order);

        //    await _context.SaveChangesAsync();

        //    return Ok(order);
        //}
        
        [HttpPost]
        public async Task<IActionResult> CreateOrder(
       Order order)
        {
            var product = await _productClient.GetProduct(
           order.ProductId);

            if (product == null)
            {
                return NotFound("Product not found");
            }

            // 2. Calculate amount
            var amount = product.Price * order.Quantity;

            order.CreatedDate = DateTime.UtcNow;
            order.Status = "Created";

            _context.Orders.Add(order);

            await _context.SaveChangesAsync();

        

            // 4. Publish event
            var orderEvent = new OrderCreatedEvent
            {
                OrderId = order.Id,
                ProductId = order.ProductId,
                Quantity = order.Quantity,
                Amount = amount
            };

            await _rabbitMqPublisher.PublishAsync(orderEvent);

            return Ok(new
            {
                OrderId = order.Id,
                Message = "Order created successfully"
            });
        }
        

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrder(int id)
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(x => x.Id == id);

            if (order == null)
                return NotFound();

            return Ok(order);
        }
        [HttpGet("test-rabbit")]
        public async Task TestRabbitMq()
        {
            var factory = new ConnectionFactory
            {
                HostName = "localhost",
                Port = 5672,
                UserName = "guest",
                Password = "guest"
            };

            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();
            Console.WriteLine("Before QueueDeclareAsync");
            await channel.QueueDeclareAsync(
                queue: "payment_queue",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            Console.WriteLine("Queue created successfully");
        }
    }
}

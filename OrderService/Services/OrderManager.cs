using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Interfaces;
using OrderService.Models;
using OrderService.Services.Clients;

namespace OrderService.Services
{
    public class OrderManager
    {
        private readonly IProductClient _productClient;
        private readonly OrderDbContext _context;

        public OrderManager(IProductClient productClient, OrderDbContext context)
        {
            _productClient = productClient;
            _context = context;
        }

        public async Task<Order> CreateOrder(int productId, int quantity)
        {
            // Call Product Service
            var product = await _productClient.GetProduct(productId);

            if (product == null)
            {
                throw new Exception("Product not found");
            }

            var order = new Order
            {
                ProductId = product.Id,
                Quantity = quantity,
                Amount = product.Price * quantity,
                Status = "Created",
                CreatedDate = DateTime.UtcNow
            };
            _context.Orders.Add(order);

            // 5. Save to SQL Server
            await _context.SaveChangesAsync();
            // Save order to database here

            return order;
        }
    }
}

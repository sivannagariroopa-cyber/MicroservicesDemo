using OrderService.Interfaces;
using OrderService.Models;

namespace OrderService.Services.Clients
{
    public class ProductClient : IProductClient
    {
        private readonly HttpClient _client;

        public ProductClient(IHttpClientFactory factory)
        {
            _client = factory.CreateClient("ProductService");
        }

        public async Task<Product?> GetProduct(int id)
        {
            return await _client.GetFromJsonAsync<Product>(
                $"api/products/{id}");
        }
    }
}

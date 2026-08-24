using OrderService.Models;

namespace OrderService.Interfaces
{
    public interface IProductClient
    {
        Task<Product?> GetProduct(int id);
    }
}

using System;
using System.Threading.Tasks;
using StuRaHsHarz.WebShop.Models;

namespace StuRaHsHarz.WebShop.Services
{
    public interface IOrderService
    {
        ValueTask<Order> OrderAsync(OrderRequest orderRequest);
        Task<Order> LoadOrderAsync(Guid guid);
    }
}

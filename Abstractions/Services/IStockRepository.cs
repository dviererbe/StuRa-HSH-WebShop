using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using StuRaHsHarz.WebShop.Models;

namespace Backend.Services
{
    public interface IStockRepository
    {
        ImmutableDictionary<ItemColor, ImmutableDictionary<ItemSize, uint>> CurrentStock { get; }

        Task RemoveFromStockAsync(IEnumerable<OrderItem> orderedItems);
    }
}
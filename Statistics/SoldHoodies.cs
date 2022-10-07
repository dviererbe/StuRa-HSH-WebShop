using System.Collections.Immutable;
using System.Threading.Tasks;
using StuRaHsHarz.WebShop.Models;

namespace StuRaHsHarz.WebShop.Statistics
{
    public record SoldHoodies
    {
        public uint Total { get; init; }
        public ImmutableDictionary<ItemColor, ImmutableDictionary<ItemSize, uint>> CountByType { get; init; } = ImmutableDictionary<ItemColor, ImmutableDictionary<ItemSize, uint>>.Empty;

        public static async Task<SoldHoodies> CalculateSoldHoodieFromCurrentStockDataAsync()
        {
            var currentStockData = await StockData.RequestCurrent();

            return CalculateSoldHoodieCount(currentStockData);
        }

        public static SoldHoodies CalculateSoldHoodieCount(ImmutableDictionary<ItemColor, ImmutableDictionary<ItemSize, uint>> stockData)
        {
            uint totalSoldHoodies = 0;
            var soldHoodiesCountByType = ImmutableDictionary.CreateBuilder<ItemColor, ImmutableDictionary<ItemSize, uint>>();

            foreach ((ItemColor itemColor, ImmutableDictionary<ItemSize, uint> originalStockDataOfItemColor) in StockData.Original)
            {
                bool itemColorIsInStock = stockData.TryGetValue(itemColor, out var newStockDataOfItemColor);

                var soldHoodieCountOfColor = ImmutableDictionary.CreateBuilder<ItemSize, uint>();

                foreach ((ItemSize itemSize, uint originalInStockAmount) in originalStockDataOfItemColor)
                {
                    if (!itemColorIsInStock || !newStockDataOfItemColor!.TryGetValue(itemSize, out uint newInStockAmount))
                    {
                        newInStockAmount = 0;
                    }

                    uint soldAmountOfItemType = originalInStockAmount - newInStockAmount;
                    totalSoldHoodies += soldAmountOfItemType;

                    soldHoodieCountOfColor.Add(itemSize, soldAmountOfItemType);
                }

                soldHoodiesCountByType.Add(itemColor, soldHoodieCountOfColor.ToImmutable());
            }

            return new SoldHoodies
            {
                Total = totalSoldHoodies,
                CountByType = soldHoodiesCountByType.ToImmutable()
            };
        }
    }
}

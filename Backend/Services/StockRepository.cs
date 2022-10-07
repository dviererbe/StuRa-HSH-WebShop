using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StuRaHsHarz.WebShop.Models;

namespace Backend.Services
{
    public class NotItemsEnoughInStock : Exception
    {
    }

    public class StockRepository : IStockRepository
    {
        private const string StockFilePath = "stock.json";

        private readonly ILogger<StockRepository> _logger;
        private ImmutableDictionary<ItemColor, ImmutableDictionary<ItemSize, uint>> _stockData;
        private readonly SemaphoreSlim _stockAccessLock;
        private readonly SemaphoreSlim _fileAccessLock;

        public StockRepository(ILogger<StockRepository> logger)
        {
            _logger = logger;

            _stockData = ImmutableDictionary<ItemColor, ImmutableDictionary<ItemSize, uint>>.Empty;
            _stockAccessLock = new SemaphoreSlim(1, 1);
            _fileAccessLock = new SemaphoreSlim(1, 1);

            LoadStockDataFromFile();
        }

        private void LoadStockDataFromFile()
        {
            string jsonContent = File.ReadAllText(StockFilePath);
            var rawStockData = JsonSerializer.Deserialize<Dictionary<ItemColor, Dictionary<ItemSize, uint>>>(jsonContent);

            if (rawStockData is not null)
            {
                var stockData = ImmutableDictionary<ItemColor, ImmutableDictionary<ItemSize, uint>>.Empty;

                foreach ((ItemColor color, IDictionary<ItemSize, uint>? colorStock) in rawStockData)
                {
                    stockData = stockData.SetItem(color, colorStock.ToImmutableDictionary());
                }

                _stockData = stockData;
            }
            else
            {
                throw new FormatException();
            }
        }

        public ImmutableDictionary<ItemColor, ImmutableDictionary<ItemSize, uint>> CurrentStock => _stockData;

        public async Task RemoveFromStockAsync(IEnumerable<OrderItem> orderedItems)
        {
            ImmutableDictionary<ItemColor, ImmutableDictionary<ItemSize, uint>> newStockData = _stockData;

            try
            {
                await _stockAccessLock.WaitAsync();
                bool changedAnything = false;

                foreach (var orderItem in orderedItems)
                {
                    if (orderItem.Amount == 0) continue;
                    
                    if (newStockData.TryGetValue(orderItem.Type.Color, out ImmutableDictionary<ItemSize, uint>? sizeStockDataForColor) &&
                        sizeStockDataForColor.TryGetValue(orderItem.Type.Size, out uint amountInStock) &&
                        amountInStock >= orderItem.Amount)
                    {
                        newStockData = newStockData.SetItem(
                            orderItem.Type.Color,
                            sizeStockDataForColor.SetItem(
                                orderItem.Type.Size, 
                                amountInStock - orderItem.Amount));

                        changedAnything = true;
                    }
                    else
                    {
                        throw new NotItemsEnoughInStock();
                    }
                }

                if (changedAnything)
                {
                    _stockData = newStockData;
                    _ = WriteChangesToFileAsync(newStockData);
                }
            }
            finally
            {
                _stockAccessLock.Release();
            }
        }

        private async Task WriteChangesToFileAsync(ImmutableDictionary<ItemColor, ImmutableDictionary<ItemSize, uint>> stockData)
        {
            try
            {
                await _fileAccessLock.WaitAsync();

                await using var stockDataFile = new FileStream(StockFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
                await JsonSerializer.SerializeAsync(stockDataFile, stockData, options: new JsonSerializerOptions()
                {
                    WriteIndented = true
                });
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to save new Stock-State!");
            }
            finally
            {
                _fileAccessLock.Release();
            }
        }
    }
}

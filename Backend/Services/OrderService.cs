using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using StuRaHsHarz.WebShop.Exceptions;
using StuRaHsHarz.WebShop.Models;
using StuRaHsHarz.WebShop.Services;

namespace Backend.Services
{
    public class OrderService : IOrderService
    {
        private const string OrdersDirectoryPath = "Orders";
        private static readonly Regex EmailPattern = new(pattern: @"\A\S+@\S+\.\S+\z", RegexOptions.Compiled);

        private readonly IStockRepository _stockRepository;

        public OrderService(IStockRepository stockRepository)
        {
            _stockRepository = stockRepository;
        }

        public async ValueTask<Order> OrderAsync(OrderRequest orderRequest)
        {
            IsValidModel(orderRequest);

            await _stockRepository.RemoveFromStockAsync(orderRequest.Items);
            
            return await SaveOrderAsync(orderRequest);
        }

        public async Task<Order> LoadOrderAsync(Guid guid)
        {
            try
            {
                await using FileStream orderDetailsFile = new(
                    path: Path.Combine(OrdersDirectoryPath, guid.ToString("B")), 
                    FileMode.Open, FileAccess.Read, FileShare.Read);

                var order = await JsonSerializer.DeserializeAsync<Order>(orderDetailsFile);
                
                return order ?? throw new FormatException("Order can't be null!");
            }
            catch (Exception exception) when (
                exception is DirectoryNotFoundException ||
                exception is FileNotFoundException)
            {
                throw new OrderNotFound();
            }
        }

        private async Task<Order> SaveOrderAsync(OrderRequest orderRequest)
        {
            if (!Directory.Exists(OrdersDirectoryPath)) Directory.CreateDirectory(OrdersDirectoryPath);

            Order order = Order.FromRequest(orderRequest);

            string orderDetailsFilePath = Path.Combine(OrdersDirectoryPath, order.Id.ToString("B"));

            await using FileStream orderDetailsFile = new(orderDetailsFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
            await JsonSerializer.SerializeAsync(orderDetailsFile, order, new JsonSerializerOptions
            {
                WriteIndented = true,
            });

            return order;
        }

        private static void IsValidModel(OrderRequest orderRequest)
        {
            IsValidName(orderRequest.Name);
            IsValidEmail(orderRequest.Email);
            
            if (orderRequest.ShippingAddress is not null)
            {
                if (orderRequest.PayCash) throw new FormatException("You can't pay cash if you specified the delivery method shipping.");
                
                IsValidAddress(orderRequest.ShippingAddress);
            }

            AreValidItems(orderRequest.Items);
        }

        private static void IsValidName(string? name)
        {
            if (name is null) throw new FormatException("Name can't be null.");
            if (name.Length == 0) throw new FormatException("Name can't be empty.");
        }

        private static void IsValidEmail(string? email)
        {
            if (email is null) throw new FormatException("E-Mail can't be null.");
            if (!EmailPattern.IsMatch(email)) throw new FormatException("E-Mail has to be a valid.");
        }

        private static void IsValidAddress(Address address)
        {
            if (address.AddressLine1 is null) throw new FormatException("AddressLine1 can't be null.");
            if (address.AddressLine1.Length == 0) throw new FormatException("AddressLine1 can't be empty.");

            if (address.AddressLine2 is null) throw new FormatException("AddressLine2 can't be null.");

            if (address.CityName is null) throw new FormatException("CityName can't be null.");
            if (address.CityName.Length == 0) throw new FormatException("CityName can't be empty.");
        }

        private static void AreValidItems(IEnumerable<OrderItem>? orderItems)
        {
            if (orderItems is null) throw new FormatException("OrderItems can't be null.");

            var isEmpty = true;
            var itemTypes = new HashSet<ItemType>();

            foreach (var orderItem in orderItems)
            {
                isEmpty = false;

                if (orderItem.Amount == 0) throw new FormatException("OrderItem Amount has to be positive.");
                if (itemTypes.Contains(orderItem.Type)) throw new FormatException("Multiple order items of ame type.");

                itemTypes.Add(orderItem.Type);
            }

            if (isEmpty) throw new FormatException("No OrderItems list is empty.");
        }
    }
}
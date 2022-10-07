using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace StuRaHsHarz.WebShop.Models
{
    public class Order
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        public Address? ShippingAddress { get; set; } = null;

        public bool PayCash { get; set; }

        public OrderState State { get; set; } = OrderState.created;

        public IEnumerable<OrderItem> Items { get; set; }

        public bool Original { get; set; } = false;

        [JsonIgnore]
        public bool Payed { get; set; } = false;

        [JsonIgnore]
        public string OrderItemsAsString
        {
            get
            {
                StringBuilder stringBuilder = new StringBuilder();

                bool first = true;

                foreach (var item in Items)
                {
                    if (!first)
                    {
                        stringBuilder.AppendLine();
                    }

                    first = false;

                    stringBuilder.Append(item.ToString());
                }

                return stringBuilder.ToString();
            }
        }

        public static Order FromRequest(OrderRequest orderRequest, Guid? orderId = null)
        {
            return new Order()
            {
                Id = orderId ?? Guid.NewGuid(),
                Name = orderRequest.Name,
                Email = orderRequest.Email,
                ShippingAddress = orderRequest.ShippingAddress,
                PayCash = orderRequest.PayCash,
                Items = orderRequest.Items
            };
        }

        public static async Task<Order> ReadFromFileAsync(string path)
        {
            await using var orderDetailsFile = new FileStream(
                path, FileMode.Open, FileAccess.Read, FileShare.Read);

            var order = await JsonSerializer.DeserializeAsync<Order>(orderDetailsFile, new JsonSerializerOptions
            {
                Converters = { new JsonStringEnumConverter() }
            });

            return order ?? throw new FormatException("Order can't be null!");
        }

        private static readonly JsonSerializerOptions SerializationOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

        public async Task WriteToFileAsync(string path)
        {
            await using var orderDetailsFile = new FileStream(
                path, FileMode.CreateNew, FileAccess.Write, FileShare.None);

            await JsonSerializer.SerializeAsync<Order>(orderDetailsFile, this, SerializationOptions);
        }

        public static Task WriteToFileAsync(string path, Order order) => order.WriteToFileAsync(path);
    }
}

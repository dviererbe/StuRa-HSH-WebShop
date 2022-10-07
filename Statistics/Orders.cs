using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;
using StuRaHsHarz.WebShop.Models;

namespace StuRaHsHarz.WebShop.Statistics
{
    public class Orders
    {
        public const string OrdersDirectory = @"R:\SturaWebshop\Orders";

        public static async Task<ImmutableList<Order>> ReadFromDirectoryAsync(string path = OrdersDirectory)
        {
            string[] filePaths = Directory.GetFiles(path);
            var orders = ImmutableList.CreateBuilder<Order>();

            foreach (string filePath in filePaths)
            {
                orders.Add(await Order.ReadFromFileAsync(filePath));
            }

            return orders.ToImmutable();
        }
    }
}

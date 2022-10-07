using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using StuRaHsHarz.WebShop.Models;
using StuRaHsHarz.WebShop.Statistics;

namespace StatisticsConsole
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var orderDetailsTask = Orders
                .ReadFromDirectoryAsync(@"R:\SturaWebshop\Orders")
                .ContinueWith(async orders => OrderDetails.FromOrders(await orders))
                .Unwrap();

            var soldHoodies = await SoldHoodies.CalculateSoldHoodieFromCurrentStockDataAsync();
            PrintSoldHoodies(soldHoodies);

            var orderDetails = await orderDetailsTask;
            PrintOrderDetails(orderDetails);
        }

        private static void PrintSoldHoodies(SoldHoodies soldHoodies)
        {
            Console.WriteLine("Total Sold Hoodies: " + soldHoodies.Total);
            Console.WriteLine("Sold Worth: " + soldHoodies.Total * 25 + ",00 EUR");
            Console.WriteLine("-----------------------------");

            foreach ((ItemColor itemColor, ImmutableDictionary<ItemSize, uint> itemColorStockData) in soldHoodies.CountByType)
            {
                Console.WriteLine(itemColor + ":");

                foreach ((ItemSize itemSize, uint soldAmount) in itemColorStockData)
                {
                    Console.WriteLine($"  - {itemSize}: {soldAmount}/{StockData.Original[itemColor][itemSize]}");
                }
            }

            Console.WriteLine();
        }

        private static void PrintOrderDetails(OrderDetails orderDetails)
        {
            Console.WriteLine($"Orders Count: {orderDetails.OrdersCount}");
            Console.WriteLine($"Count of Orders to be delivered: {orderDetails.DhlOrdersCount}");
            Console.WriteLine($"Count of Orders which have to be picked up: {orderDetails.SelfPickupOrdersCount}");
            Console.WriteLine($"Count of Orders payed via Bank transfer: {orderDetails.PayedInAdvanceOrderCount}");
            Console.WriteLine($"Count of Orders payed Cash: {orderDetails.PayedCashOrderCount}");

            PrintDictionary(orderDetails.OrderSizeCount, "Count of Orders by Item-Count in Order");
            PrintDictionary(orderDetails.OrderSizeCountOfDeliveries, "Count of Orders by Item-Count in Order (only orders to be delivered)");
            PrintDictionary(orderDetails.OrderSizeCountOfSefPickup, "Count of Orders by Item-Count in Order (only orders to be picked up)");

            static void PrintDictionary(IDictionary<uint, uint> dictionary, string heading)
            {
                Console.WriteLine();
                Console.WriteLine(heading + ":");
                Console.WriteLine("----------------------------------------");

                foreach ((uint itemCount, uint count) in dictionary)
                {
                    Console.WriteLine($"{count} x {itemCount}");
                }
            }
        }
    }
}

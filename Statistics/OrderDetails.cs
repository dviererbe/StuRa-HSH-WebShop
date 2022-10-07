using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using StuRaHsHarz.WebShop.Models;

namespace StuRaHsHarz.WebShop.Statistics
{
    public record OrderDetails
    {
        public uint OrdersCount => (uint)Orders.Count;

        public uint PayedCashOrderCount { get; init; }

        public uint PayedInAdvanceOrderCount { get; init; }

        public uint DhlOrdersCount { get; init; }

        public uint SelfPickupOrdersCount { get; init; }

        public ImmutableSortedDictionary<uint, uint> OrderSizeCount { get; init; } = ImmutableSortedDictionary<uint, uint>.Empty;

        public ImmutableSortedDictionary<uint, uint> OrderSizeCountOfSefPickup { get; init; } = ImmutableSortedDictionary<uint, uint>.Empty;

        public ImmutableSortedDictionary<uint, uint> OrderSizeCountOfDeliveries { get; init; } = ImmutableSortedDictionary<uint, uint>.Empty;

        public ImmutableList<Order> Orders { get; init; } = ImmutableList<Order>.Empty;

        public static OrderDetails FromOrders(IEnumerable<Order> orders)
        {
            
            uint payedCashOrderCount = 0;
            uint payedInAdvanceOrderCount = 0;
            uint dhlOrdersCount = 0;
            uint selfPickupOrdersCount = 0;
            var orderSizeCount = ImmutableSortedDictionary.CreateBuilder<uint, uint>();
            var orderSizeCountOfSefPickup = ImmutableSortedDictionary.CreateBuilder<uint, uint>();
            var orderSizeCountOfDeliveries = ImmutableSortedDictionary.CreateBuilder<uint, uint>();

            Action<Order> addOrderToList; 
            Func<ImmutableList<Order>> getImmutableOrderList;
            ImmutableList<Order>.Builder? immutableOrderListBuilder;

            if (orders is ImmutableList<Order> immutableOrderList)
            {
                addOrderToList = DoNothing;
                getImmutableOrderList = () => immutableOrderList;
            }
            else
            {
                immutableOrderListBuilder = ImmutableList.CreateBuilder<Order>();
                addOrderToList = order => immutableOrderListBuilder.Add(order);
                getImmutableOrderList = () => immutableOrderListBuilder.ToImmutable();
            }

            foreach (var order in orders)
            {
                addOrderToList(order);

                uint orderedItemsCount = GetOrderCountOfOrder(order);
                IncrementOrderedItemsCountToDictionary(orderedItemsCount, orderSizeCount);

                if (order.PayCash)
                {
                    ++payedCashOrderCount;
                }
                else
                {
                    ++payedInAdvanceOrderCount;
                }

                if (order.ShippingAddress is null)
                {
                    ++selfPickupOrdersCount;
                    IncrementOrderedItemsCountToDictionary(orderedItemsCount, orderSizeCountOfSefPickup);
                }
                else
                {
                    ++dhlOrdersCount;
                    IncrementOrderedItemsCountToDictionary(orderedItemsCount, orderSizeCountOfDeliveries);
                }
            }

            return new OrderDetails
            {
                Orders = getImmutableOrderList(),
                PayedCashOrderCount = payedCashOrderCount,
                PayedInAdvanceOrderCount = payedInAdvanceOrderCount,
                DhlOrdersCount = dhlOrdersCount,
                SelfPickupOrdersCount = selfPickupOrdersCount,
                OrderSizeCount = orderSizeCount.ToImmutable(),
                OrderSizeCountOfSefPickup = orderSizeCountOfSefPickup.ToImmutable(),
                OrderSizeCountOfDeliveries = orderSizeCountOfDeliveries.ToImmutable(),
            };

            static void IncrementOrderedItemsCountToDictionary(uint orderedItemsCount, IDictionary<uint, uint> dictionary)
            {
                if (dictionary.TryGetValue(orderedItemsCount, out uint totalOrderedItemsCount))
                {
                    dictionary[orderedItemsCount] = totalOrderedItemsCount + 1;
                }
                else
                {
                    dictionary.Add(orderedItemsCount, 1);
                }
            }
        }

        private static void DoNothing(Order order)
        {
        }

        private static uint GetOrderCountOfOrder(Order order)
        {
            uint totalCount = 0;

            foreach (var orderItem in order.Items)
            {
                totalCount += orderItem.Amount;
            }

            return totalCount;
        }
    }
}

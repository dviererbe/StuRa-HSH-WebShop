using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using StuRaHsHarz.WebShop.Models;

namespace StuRaHsHarz.WebShop.Statistics
{
    public class StockData
    {
        public static readonly ImmutableDictionary<ItemColor, ImmutableDictionary<ItemSize, uint>> Original = new Dictionary<ItemColor, ImmutableDictionary<ItemSize, uint>>()
        {
            {
                ItemColor.BLACK,
                new Dictionary<ItemSize, uint>
                {
                    { ItemSize.XS, 22 },
                    { ItemSize.S, 29 },
                    { ItemSize.M, 38 },
                    { ItemSize.L, 15 },
                    { ItemSize.XL, 21 },
                    { ItemSize.XXL, 17 },
                    { ItemSize.XXXL, 5 },
                }.ToImmutableDictionary()
            },
            {
                ItemColor.GREY,
                new Dictionary<ItemSize, uint>
                {
                    { ItemSize.XS, 21 },
                    { ItemSize.S, 34 },
                    { ItemSize.M, 26 },
                    { ItemSize.L, 9 },
                    { ItemSize.XL, 23 },
                    { ItemSize.XXL, 19 },
                    { ItemSize.XXXL, 5 },
                }.ToImmutableDictionary()
            },
            {
                ItemColor.BLUE,
                new Dictionary<ItemSize, uint>
                {
                    { ItemSize.XS, 7 },
                    { ItemSize.S, 12 },
                    { ItemSize.M, 1 },
                    { ItemSize.L, 21 },
                    { ItemSize.XL, 10 },
                    { ItemSize.XXL, 21 },
                    { ItemSize.XXXL, 3 },
                }.ToImmutableDictionary()
            },
            {
                ItemColor.RED,
                new Dictionary<ItemSize, uint>
                {
                    { ItemSize.XS, 13 },
                    { ItemSize.S, 27 },
                    { ItemSize.M, 6 },
                    { ItemSize.L, 8 },
                    { ItemSize.XL, 18 },
                    { ItemSize.XXL, 20 },
                    { ItemSize.XXXL, 5 },
                }.ToImmutableDictionary()
            },
        }.ToImmutableDictionary();

        public static async Task<ImmutableDictionary<ItemColor, ImmutableDictionary<ItemSize, uint>>> RequestCurrent(CancellationToken cancellationToken = default)
        {
            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync("https://sturashopbackend.azurewebsites.net/stock", cancellationToken);

            return response.StatusCode is HttpStatusCode.OK
                ? await DeserializeStockDataAsync(response.Content, cancellationToken)
                : throw new Exception("Requesting Stock Data didn't succeeded.");
        }

        private static async Task<ImmutableDictionary<ItemColor, ImmutableDictionary<ItemSize, uint>>> DeserializeStockDataAsync(HttpContent responseContent, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using var contentStream = await responseContent.ReadAsStreamAsync(cancellationToken);

            var deserializedContent = await JsonSerializer.DeserializeAsync<Dictionary<ItemColor, Dictionary<ItemSize, uint>>>(contentStream, cancellationToken: cancellationToken);

            return deserializedContent!.ToImmutableDictionary(
                keySelector: item => item.Key,
                elementSelector: item => item.Value.ToImmutableDictionary());
        }
    }
}

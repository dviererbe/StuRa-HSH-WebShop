using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using iTextSharp.text.pdf;
using Org.BouncyCastle.Asn1.Cms;
using StuRaHsHarz.WebShop.Models;
using StuRaHsHarz.WebShop.Statistics;

namespace Sandbox
{
    class Program
    {
        private static readonly Guid[] notInStock = new Guid[]
        {
            Guid.Parse("{a755ea0f-d26d-4407-bf83-77c00834b219}"),
            Guid.Parse("{c748a73d-0f6a-49d9-a10d-58e8d5e25ebc}"),
            Guid.Parse("{ab3ed211-dfe5-4a3f-abf4-151ce0715195}"),
            Guid.Parse("{bdb64378-8e96-429b-a06c-edfb151d000a}"),
            Guid.Parse("{14ed97c7-65e0-4911-836a-9f3bab5f5433}"),
            Guid.Parse("{19b1a052-5111-42a8-85c8-f644716dfc6c}"),
            Guid.Parse("{efa0bb0f-342b-4b57-b76a-4eb8daec6a40}"),
            Guid.Parse("{09303d84-64fb-43ca-8200-eecbbbc70f08}"),
            Guid.Parse("{1c90b98f-6e40-4186-bd77-8ec2b06736c7}"),
            Guid.Parse("{2ab0f820-de47-4ca5-9e6b-3a77215f143d}"),
            Guid.Parse("{603fa2b6-48cd-4df4-a7ee-ff914d798726}"),
        };

        public static ImmutableDictionary<ItemColor, ImmutableDictionary<ItemSize, uint>> Stock = new Dictionary<ItemColor, ImmutableDictionary<ItemSize, uint>>()
        {
            {
                ItemColor.BLACK,
                new Dictionary<ItemSize, uint>
                {
                    { ItemSize.XS, 21 },
                    { ItemSize.S, 26 },
                    { ItemSize.M, 25 },
                    { ItemSize.L, 10 },
                    { ItemSize.XL, 18 },
                    { ItemSize.XXL, 15 },
                    { ItemSize.XXXL, 2 },
                }.ToImmutableDictionary()
            },
            {
                ItemColor.GREY,
                new Dictionary<ItemSize, uint>
                {
                    { ItemSize.XS, 21 },
                    { ItemSize.S, 29 },
                    { ItemSize.M, 16 },
                    { ItemSize.L, 18 },
                    { ItemSize.XL, 21 },
                    { ItemSize.XXL, 17 },
                    { ItemSize.XXXL, 5 },
                }.ToImmutableDictionary()
            },
            {
                ItemColor.BLUE,
                new Dictionary<ItemSize, uint>
                {
                    { ItemSize.XS, 7 },
                    { ItemSize.S, 5 },
                    { ItemSize.M, 0 },
                    { ItemSize.L, 0 },
                    { ItemSize.XL, 5 },
                    { ItemSize.XXL, 12 },
                    { ItemSize.XXXL, 2 },
                }.ToImmutableDictionary()
            },
            {
                ItemColor.RED,
                new Dictionary<ItemSize, uint>
                {
                    { ItemSize.XS, 10 },
                    { ItemSize.S, 20 },
                    { ItemSize.M, 2 },
                    { ItemSize.L, 3 },
                    { ItemSize.XL, 12 },
                    { ItemSize.XXL, 18 },
                    { ItemSize.XXXL, 4 },
                }.ToImmutableDictionary()
            },
        }.ToImmutableDictionary();


        static void PrintOutOrders(ImmutableList<Order> orders)
        {
            string[] guidStrings = new[]
            {
                "{5ce4574d-eded-4793-a267-709902962de8}",
                "{89e795dd-7b84-4be6-abbc-434cc4a9970c}",
                "{52ddd4ef-9dc5-4f87-a67d-6e2993cffe1c}",
                "{9c9e3548-7ece-43ae-98aa-27c6e0db2c15}",
            };

            Guid[] guids = new Guid[guidStrings.Length];

            for (int i = 0; i < guidStrings.Length; ++i)
            {
                guids[i] = Guid.Parse(guidStrings[i]);
            }

            foreach (Order order in orders)
            {
                bool isOneOf = false;

                foreach (Guid guid in guids)
                {
                    if (order.Id.Equals(guid))
                    {
                        isOneOf = true;
                        break;
                    }
                }

                if (isOneOf)
                {
                    PrintOrder(order);
                }
            }

        }

        static async Task<ImmutableList<Guid>> ReadOrderIdsListAsync(string filePath, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var orderIds = ImmutableList.CreateBuilder<Guid>();

            await using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (StreamReader fileStreamReader = new StreamReader(fileStream, Encoding.UTF8))
                {
                    while (!fileStreamReader.EndOfStream)
                    {
                        string line = await fileStreamReader.ReadLineAsync() ?? throw new DataException("Unexpected null read!");
                        
                        Guid orderId = Guid.Parse(line);

                        if (orderIds.Contains(orderId))
                        {
                            throw new DataException($"Duplicate OrderId: {orderId:B}");
                        }
                        else
                        {
                            orderIds.Add(orderId);
                        }
                    }
                }
            }

            return orderIds.ToImmutable();
        }

        static ImmutableDictionary<Guid, Order> IndexOrders(IEnumerable<Order> orders)
        {
            var orderLookupTable = ImmutableDictionary.CreateBuilder<Guid, Order>();

            foreach (Order order in orders)
            {
                orderLookupTable.Add(order.Id, order);
            }

            return orderLookupTable.ToImmutable();
        }

        private static readonly Regex HouseNumberPattern = new Regex(@"\A\s*[0-9]+[^0-9]*\z", RegexOptions.Compiled);
        private static readonly Regex AddressLinePattern = new Regex(@"\A(?<Street>[^0-9]+)(?<HouseNo>[0-9].*)\z", RegexOptions.Compiled);

        static void ParseDetailsFromAddress(Address address, out string street, out string houseno, out string name2)
        {
            Match match = AddressLinePattern.Match(address.AddressLine1);

            if (match.Success)
            {
                street = match.Groups["Street"].Value.Trim();
                houseno = match.Groups["HouseNo"].Value.Trim();
                name2 = address.AddressLine2.Trim();
            }
            else if (HouseNumberPattern.IsMatch(address.AddressLine2))
            {
                street = address.AddressLine1.Trim();
                houseno = address.AddressLine2.Trim();
                name2 = String.Empty;
            }
            else
            {
                if (address.AddressLine1.Contains("Klötze"))
                {
                    street = address.AddressLine1;
                    name2 = address.AddressLine2;
                    houseno = string.Empty;
                    
                }
                else
                {
                    throw new FormatException();
                }
            }
        }

        static async Task OrdersToCsvAsync(IEnumerable<Order> orders, string path)
        {
            char csvSeparator = ';';
            string newLine = Environment.NewLine;
            StringBuilder csv = new StringBuilder();

            csv.Append("Id").Append(csvSeparator)
                .Append("Name").Append(csvSeparator)
                .Append("Email").Append(csvSeparator)
                .Append("Selbstabholung").Append(csvSeparator)
                .Append("AddressLine1" ?? string.Empty).Append(csvSeparator)
                .Append("AddressLine2" ?? string.Empty).Append(csvSeparator)
                .Append("Postleitzahl").Append(csvSeparator)
                .Append("Stadt").Append(csvSeparator)
                .Append("Order State").Append(csvSeparator)
                .Append("Barzahlung").Append(csvSeparator)
                .Append("Item 1 Amount").Append(csvSeparator)
                .Append("Item 1 Size").Append(csvSeparator)
                .Append("Item 1 Color").Append(csvSeparator)
                .Append("Item 2 Amount").Append(csvSeparator)
                .Append("Item 2 Size").Append(csvSeparator)
                .Append("Item 2 Color").Append(csvSeparator)
                .Append("Item 3 Amount").Append(csvSeparator)
                .Append("Item 3 Size").Append(csvSeparator)
                .Append("Item 3 Color").Append(csvSeparator)
                .Append("Original").Append(newLine);

            foreach (Order order in orders)
            {
                OrderItem[] orderedItems = order.Items.ToArray();

                csv.Append(order.Id.ToString("B")).Append(csvSeparator)
                    .Append(order.Name).Append(csvSeparator)
                    .Append(order.Email).Append(csvSeparator)
                    .Append(order.ShippingAddress is null ? "TRUE" : "FALSE").Append(csvSeparator)
                    .Append(order.ShippingAddress?.AddressLine1 ?? string.Empty).Append(csvSeparator)
                    .Append(order.ShippingAddress?.AddressLine2 ?? string.Empty).Append(csvSeparator)
                    .Append(order.ShippingAddress?.PostalCode.ToString().PadLeft(5, '0') ?? string.Empty).Append(csvSeparator)
                    .Append(order.ShippingAddress?.CityName ?? string.Empty).Append(csvSeparator)
                    .Append(order.State.ToString()).Append(csvSeparator)
                    .Append(order.PayCash ? "TRUE" : "FALSE").Append(csvSeparator)
                    .Append(orderedItems[0].Amount).Append(csvSeparator)
                    .Append(orderedItems[0].Type.Size).Append(csvSeparator)
                    .Append(orderedItems[0].Type.Color).Append(csvSeparator)
                    .Append(orderedItems.Length > 1 ? orderedItems[1].Amount.ToString() : string.Empty).Append(csvSeparator)
                    .Append(orderedItems.Length > 1 ? orderedItems[1].Type.Size.ToString() : string.Empty).Append(csvSeparator)
                    .Append(orderedItems.Length > 1 ? orderedItems[1].Type.Color.ToString() : string.Empty).Append(csvSeparator)
                    .Append(orderedItems.Length > 2 ? orderedItems[2].Amount.ToString() : string.Empty).Append(csvSeparator)
                    .Append(orderedItems.Length > 2 ? orderedItems[2].Type.Size.ToString() : string.Empty).Append(csvSeparator)
                    .Append(orderedItems.Length > 2 ? orderedItems[2].Type.Color.ToString() : string.Empty).Append(csvSeparator)
                    .Append(order.Original ? "TRUE" : "FALSE").Append(newLine);
            }
            
            await File.WriteAllTextAsync(
                path: Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Orders.csv"),
                contents: csv.ToString(),
                encoding: Encoding.UTF8);
        }

        static async Task Main(string[] args)
        {
            var lookupTable = await ParseOrderCsvAsync(path: @"C:\Users\Dominik\Downloads\Bestellungen.CSV");

            int sold = 0;
            int shipped = 0;

            foreach (Order order in lookupTable.Values)
            {
                if (order.State is OrderState.cancelled) continue;
                if (!order.Payed) continue;


            }


        }

        static IEnumerable<Guid> Filter(IEnumerable<Order> orders)
        {
            foreach (Order order in orders)
            {
                bool shipping = order.ShippingAddress is not null;

                if (shipping &&
                    order.Payed && 
                    order.State == OrderState.created)
                {
                    yield return order.Id;
                }
            }
        }

        static async Task Main3(string[] args)
        {
            var orders = await Orders.ReadFromDirectoryAsync(@"R:\SturaWebshop\EditedOrder");
            var orderIds = await ReadOrderIdsListAsync(@"C:\Users\Dominik\Documents\Ids");
            var lookupTable = IndexOrders(orders);

            
            Console.WriteLine("BEGIN");

            StringBuilder sb = new StringBuilder();
            foreach (Guid orderId in orderIds)
            {
                var order = lookupTable[orderId];
                sb.Append(order.Email).Append("; ");


                PrintOrder(order);
            }

            Console.WriteLine();
            Console.WriteLine(sb.ToString());
        }

        static async Task Main2(string[] args)
        {
            var orders = await Orders.ReadFromDirectoryAsync();
            var orderLookupTable = IndexOrders(orders);

            var shipping1Ids = await ReadOrderIdsListAsync(
                filePath: Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Versand - 1.txt"));

            var shipping2Ids = await ReadOrderIdsListAsync(
                filePath: Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Versand - 2.txt"));

            var newOrders = await Orders.ReadFromDirectoryAsync(@"C:\Users\Dominik\Documents\Orders");

            
            //await CreateDhlShippingCsvAsync(orderLookupTable, orderIds);

            string targetDir = @"R:\SturaWebshop\EditedOrder";

            for (int i = 0; i < newOrders.Count; ++i)
            {
                Order order = newOrders[i];

                if (orderLookupTable.ContainsKey(order.Id))
                {
                    order = orderLookupTable[order.Id];
                    order.Original = true;

                    if (shipping1Ids.Contains(order.Id) ||
                        shipping2Ids.Contains(order.Id))
                    {
                        order.State = OrderState.delivered;
                    }
                }
                else
                {
                    order.Original = false;
                }

                await order.WriteToFileAsync(Path.Join(targetDir, $"{order.Id:B}.json"));
            }

            /*
            var orders = await Orders.ReadFromDirectoryAsync();

            //PrintOutOrders(orders);

            foreach (Order order in orders)
            {
                if (Regex.IsMatch(order.Name, "ominik", RegexOptions.IgnoreCase))
                {
                    PrintOrder(order);
                }

                
                if (!IsLooser(order) && !(order.ShippingAddress is null))
                {
                    PrintOrder(order);
                    PrintToPdf(order);
                }
                
            }*/


            //PrintStock();
        }

        private static async Task<ImmutableDictionary<Guid, Order>> ParseOrderCsvAsync(string path)
        {
            var orderDictionary = ImmutableDictionary.CreateBuilder<Guid, Order>();

            await using FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            using StreamReader streamReader = new StreamReader(fileStream, Encoding.Latin1);

            //Read Headings
            await streamReader.ReadLineAsync();

            while (!streamReader.EndOfStream)
            {
                string line = (await streamReader.ReadLineAsync())!;
                Order order = ParseOrderFromCsvLine(line);
                orderDictionary.Add(order.Id, order);
            }
            
            return orderDictionary.ToImmutable();
        }
        
        private static Order ParseOrderFromCsvLine(string csvLine)
        {
            string[] values = csvLine.Split(';');

            if (values.Length != 22) throw new FormatException();

            return new Order()
            {
                Id = Guid.Parse(values[0]),
                Name = values[1],
                Email = values[2],
                ShippingAddress = ParseAddress(),
                State = ParseOrderState(),
                PayCash = MapBool(values[9]),
                Items = ParseOrderItems(),
                Original = MapBool(values[20]),
                Payed = MapBool(values[19]),
            };

            bool MapBool(string value)
            {
                if (value.Equals("WAHR", StringComparison.OrdinalIgnoreCase)) return true;
                if (value.Equals("FALSCH", StringComparison.OrdinalIgnoreCase)) return false;

                throw new DataException();
            }

            Address? ParseAddress()
            {
                if (MapBool(values[3]))
                {
                    return null;
                }
                else
                {
                    return new Address()
                    {
                        AddressLine1 = values[4],
                        AddressLine2 = values[5],
                        CityName = values[7],
                        PostalCode = uint.Parse(values[6]),
                    };
                }
            }

            OrderState ParseOrderState()
            {
                string orderState = values[8];

                if (orderState.Equals("cancelled", StringComparison.OrdinalIgnoreCase) ||
                    orderState.Equals("canceled", StringComparison.OrdinalIgnoreCase))
                {
                    return OrderState.cancelled;
                }
                else if(orderState.Equals("delivered", StringComparison.OrdinalIgnoreCase))
                {
                    return OrderState.delivered;
                }
                else if (orderState.Equals("created", StringComparison.OrdinalIgnoreCase))
                {
                    return OrderState.created;
                }
                else if (orderState.Equals("paymentReceived", StringComparison.OrdinalIgnoreCase))
                {
                    return OrderState.paymentReceived;
                }
                else if (orderState.Equals("pending", StringComparison.OrdinalIgnoreCase))
                {
                    return OrderState.pending;
                }
                else if (orderState.Equals("shipped", StringComparison.OrdinalIgnoreCase))
                {
                    return OrderState.shipped;
                }
                else if (orderState.Equals("shipping", StringComparison.OrdinalIgnoreCase))
                {
                    return OrderState.shipping;
                }
                else
                {
                    throw new DataException();
                }
            }

            IEnumerable<OrderItem> ParseOrderItems()
            {
                OrderItem? orderItem1 = ParseOrderItem(values[10], values[11], values[12]);
                OrderItem? orderItem2 = ParseOrderItem(values[13], values[14], values[15]);
                OrderItem? orderItem3 = ParseOrderItem(values[16], values[17], values[18]);

                List<OrderItem> orderItems = new List<OrderItem>();

                if (orderItem1 is not null) orderItems.Add(orderItem1);
                if (orderItem2 is not null) orderItems.Add(orderItem2);
                if (orderItem3 is not null) orderItems.Add(orderItem3);

                if (orderItems.Count == 0) throw new DataException();

                return orderItems;
            }

            static OrderItem? ParseOrderItem(string itemAmount, string itemSize, string itemColor)
            {
                if (string.IsNullOrWhiteSpace(itemAmount)) return null;

                return new OrderItem()
                {
                    Amount = uint.Parse(itemAmount),
                    Type = ParseItemType(itemSize, itemColor),
                };
            }

            static ItemType ParseItemType(string itemSize, string itemColor)
            {
                return new ItemType()
                {
                    Color = ParseItemColor(itemColor),
                    Size = ParseItemSize(itemColor),
                };
            }

            static ItemSize ParseItemSize(string itemSize)
            {
                if (itemSize.Equals("XS", StringComparison.OrdinalIgnoreCase)) return ItemSize.XS;
                if (itemSize.Equals("S", StringComparison.OrdinalIgnoreCase)) return ItemSize.S;
                if (itemSize.Equals("M", StringComparison.OrdinalIgnoreCase)) return ItemSize.M;
                if (itemSize.Equals("L", StringComparison.OrdinalIgnoreCase)) return ItemSize.L;
                if (itemSize.Equals("XL", StringComparison.OrdinalIgnoreCase)) return ItemSize.XL;
                if (itemSize.Equals("XXL", StringComparison.OrdinalIgnoreCase)) return ItemSize.XXL;
                if (itemSize.Equals("XXXL", StringComparison.OrdinalIgnoreCase)) return ItemSize.XXXL;

                throw new DataException();
            }

            static ItemColor ParseItemColor(string itemColor)
            {
                if (itemColor.Equals("BLACK", StringComparison.OrdinalIgnoreCase)) return ItemColor.BLACK;
                if (itemColor.Equals("GREY", StringComparison.OrdinalIgnoreCase)) return ItemColor.GREY;
                if (itemColor.Equals("BLUE", StringComparison.OrdinalIgnoreCase)) return ItemColor.BLUE;
                if (itemColor.Equals("RED", StringComparison.OrdinalIgnoreCase)) return ItemColor.RED;

                throw new DataException();
            }
        }

        private static Task CreateDhlShippingCsvAsync(
            IReadOnlyDictionary<Guid, Order> orderLookupTable)
        {
            return CreateDhlShippingCsvAsync(orderLookupTable, orderLookupTable.Keys);
        }

        private static async Task CreateDhlShippingCsvAsync(
            IReadOnlyDictionary<Guid, Order> orderLookupTable,
            IEnumerable<Guid> orderIds)
        {
            char csvSeparator = ';';
            string newLine = Environment.NewLine;
            StringBuilder csv = new StringBuilder();

            csv.Append(";DHL Online Frankierung;;;;;;;;;;;;;;;;").Append(newLine)
                .Append(";CSV Import Beispiel;;;;;;;;;;;;;;;;").Append(newLine)
                .Append(";;;;;;;;;;;;;;;;;").Append(newLine)
                .Append(";Passen Sie die Adressdaten an und speichern Sie dieses Blatt als CSV-Datei (Datei / Speichern unter / Dateityp: CSV);;;;;;;;;;;;;;;;").Append(newLine)
                .Append(";;;;;;;;;;;;;;;;;").Append(newLine)
                .Append(";;;;;;;;;;;;;;;;;").Append(newLine);

            csv.Append("Erläuterungen").Append(csvSeparator)
                .Append("SEND_NAME1").Append(csvSeparator)
                .Append("SEND_NAME2").Append(csvSeparator)
                .Append("SEND_STREET").Append(csvSeparator)
                .Append("SEND_HOUSENUMBER").Append(csvSeparator)
                .Append("SEND_PLZ").Append(csvSeparator)
                .Append("SEND_CITY").Append(csvSeparator)
                .Append("SEND_COUNTRY").Append(csvSeparator)
                .Append("RECV_NAME1").Append(csvSeparator)
                .Append("RECV_NAME2").Append(csvSeparator)
                .Append("RECV_STREET").Append(csvSeparator)
                .Append("RECV_HOUSENUMBER").Append(csvSeparator)
                .Append("RECV_PLZ").Append(csvSeparator)
                .Append("RECV_CITY").Append(csvSeparator)
                .Append("RECV_COUNTRY").Append(csvSeparator)
                .Append("PRODUCT").Append(csvSeparator)
                .Append("COUPON").Append(csvSeparator)
                .Append("SEND_EMAIL").Append(newLine);

            foreach (Guid orderId in orderIds)
            {
                Order order = orderLookupTable[orderId];
                Address shippingAddress = order.ShippingAddress!;

                string name1 = order.Name;

                string city = shippingAddress.CityName;
                string plz = shippingAddress.PostalCode.ToString().PadLeft(5, '0');

                ParseDetailsFromAddress(
                    shippingAddress,
                    out string street,
                    out string houseno,
                    out string name2);

                csv.Append(string.Empty).Append(csvSeparator);

                AppendSendAddress(csv, csvSeparator);

                csv.Append(name1).Append(csvSeparator)
                    .Append(name2).Append(csvSeparator)
                    .Append(street).Append(csvSeparator)
                    .Append(houseno).Append(csvSeparator)
                    .Append(plz).Append(csvSeparator)
                    .Append(city).Append(csvSeparator)
                    .Append("DEU").Append(csvSeparator) //country
                    .Append("PAK02.DEU").Append(csvSeparator) //product
                    .Append("").Append(csvSeparator) //coupon
                    .Append("dviererbe-stura@hs-harz.de").Append(csvSeparator) //send mail
                    .Append(newLine);
            }

            await File.WriteAllTextAsync(
                path: Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DHL.csv"),
                contents: csv.ToString(),
                encoding: Encoding.Latin1);
        }

        private static void AppendSendAddress(StringBuilder stringBuilder, char seperator)
        {
            stringBuilder
                .Append("Studierendenrat der Hochschule Harz").Append(seperator) //name1
                .Append("").Append(seperator) //name2
                .Append("Friedrichstraße").Append(seperator) //street
                .Append("57-59").Append(seperator) //houseno
                .Append("38855").Append(seperator) //plz
                .Append("Wernigerode").Append(seperator) //city
                .Append("DEU").Append(seperator);
        }

        private static void PrintToPdf(Order order)
        {
            Directory.CreateDirectory("PDFs");

            using (Stream pdfInputStream = new FileStream(path: @"C:\Users\Dominik\Desktop\StuRa_HoodySaleLieferschein_Formularfelder.pdf", mode: FileMode.Open))
            using (Stream resultPDFOutputStream = new FileStream(path: @$"Pdfs\{order.Id:B}.pdf", mode: FileMode.Create))
            using (Stream resultPDFStream = FillForm(pdfInputStream, order))
            {
                // set the position of the stream to 0 to avoid corrupted PDF. 
                resultPDFStream.Position = 0;
                resultPDFStream.CopyTo(resultPDFOutputStream);
            }
        }

        public static Stream FillForm(Stream inputStream, Order order)
        {
            Stream outStream = new MemoryStream();
            PdfReader pdfReader = null;
            PdfStamper pdfStamper = null;
            Stream inStream = null;
            try
            {
                string addressLine = order.ShippingAddress.AddressLine1;

                if (order.ShippingAddress.AddressLine2.Length > 0)
                {
                    addressLine += "; " + order.ShippingAddress.AddressLine2;
                }

                pdfReader = new PdfReader(inputStream);
                pdfStamper = new PdfStamper(pdfReader, outStream);
                AcroFields form = pdfStamper.AcroFields;
                form.SetField("Name", order.Name);
                form.SetField("Str. + Hausnr", addressLine);
                form.SetField("PLZ + Ort",
                    order.ShippingAddress.PostalCode.ToString().PadLeft(5, '0') + " " + order.ShippingAddress.CityName);
                form.SetField("Bestellnummer", order.Id.ToString("N"));

                var enumerator = order.Items.GetEnumerator();

                int sum = 0;

                bool notEnd = false;

                FillForm(form, enumerator.MoveNext() ? enumerator.Current : null, 1, ref sum);
                FillForm(form, enumerator.MoveNext() ? enumerator.Current : null, 2, ref sum);
                FillForm(form, enumerator.MoveNext() ? enumerator.Current : null, 3, ref sum);

                form.SetField("Gesamtzahl Hoodys Paket", sum.ToString());
                form.SetField("Gesamtsumme Hoodys Paket", $"{sum * 25},00€");

                pdfStamper.FormFlattening = true;
                return outStream;
            }
            finally
            {
                pdfStamper?.Close();
                pdfReader?.Close();
                inStream?.Close();
            }
        }

        private static void FillForm(AcroFields form, OrderItem? orderItem, int n, ref int sum)
        {
            string farbe = string.Empty;
            string anzahl = string.Empty;
            string größe = string.Empty;
            string summe = string.Empty;

            if (!(orderItem is null))
            {
                farbe = orderItem.Type.Color.ToString();
                anzahl = orderItem.Amount.ToString();
                größe = orderItem.Type.Size.ToString();
                summe = $"{orderItem.Amount * 25},00€";

                sum += (int)orderItem.Amount;
            }

            form.SetField("Farbe " + n, farbe);
            form.SetField("Anzahl " + n, anzahl);
            form.SetField("Größe " + n, größe);
            form.SetField("Summe " + n, summe);


        }

        private static void PrintStock()
        {
            Console.WriteLine();
            Console.WriteLine("Stock:");
            Console.WriteLine("------");

            foreach ((ItemColor color, ImmutableDictionary<ItemSize, uint> stockOfColor) in Stock)
            {
                Console.WriteLine(color + ": ");

                foreach ((ItemSize size, uint amount) in stockOfColor)
                {
                    Console.WriteLine($" - {amount} x {size}");
                }
            }
        }

        private static bool TryRemoveFromStock(IEnumerable<OrderItem> orderedItems)
        {
            var newStock = Stock;

            foreach (OrderItem orderedItem in orderedItems)
            {
                try
                {
                    RemoveFromStock(orderedItem, ref newStock);
                }
                catch
                {
                    return false;
                }
            }

            Stock = newStock;
            return true;
        }

        private static void RemoveFromStock(
            OrderItem orderItem, ref ImmutableDictionary<ItemColor, ImmutableDictionary<ItemSize, uint>> stock)
        {
            uint inStock = stock[orderItem.Type.Color][orderItem.Type.Size];

            if (inStock < orderItem.Amount)
            {
                throw new Exception();
            }

            stock = stock.SetItem(
                key: orderItem.Type.Color,
                value: stock[orderItem.Type.Color].SetItem(
                    key: orderItem.Type.Size, 
                    value: inStock - orderItem.Amount));
        }

        private static bool OneOfNotInStock(Order order)
        {
            if (order.ShippingAddress is null)
            {
                if (order.Id.Equals(Guid.Parse("{dcfd968a-5103-4619-ba3e-e0f2b3d05af3}"))) return false;
                if (order.Id.Equals(Guid.Parse("{8ad2a45a-74de-4bea-a711-0ccfdb932bbf}"))) return false;

                if (!TryRemoveFromStock(order.Items))
                    return true;
            }
            else
            {
                foreach (Guid orderId in notInStock)
                {
                    if (orderId.Equals(order.Id)) return true;
                }
            }

            return false;
        }

        private static bool IsLooser(Order order)
        {
            if (OneOfNotInStock(order)) return true;

            return false;
        }

        private static bool ContainsBlueMorL(Order order)
        {
            foreach (OrderItem orderItem in order.Items)
            {
                if (orderItem.Type.Color is ItemColor.BLUE &&
                    (orderItem.Type.Size is ItemSize.M || orderItem.Type.Size is ItemSize.L))
                {
                    return true;
                }
            }

            return false;
        }

        private static void PrintOrder(Order order)
        {
            Console.WriteLine($"Order-ID: {order.Id:B}");
            Console.WriteLine($"Name: {order.Name}");
            Console.WriteLine($"Email: {order.Email}");
            Console.WriteLine($"Self-Pickup: {order.ShippingAddress is null}");
            Console.WriteLine($"PayCash: {order.PayCash}");
            Console.WriteLine($"Ordered-Items:");
            Console.WriteLine(order.OrderItemsAsString);
            Console.WriteLine();
            //Console.Write($"{order.Email}; ");
        }
    }
}

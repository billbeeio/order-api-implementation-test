using System;
using System.Collections.Generic;
using System.Linq;

namespace Minimal_Order_Api
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            IOrderAPI api = null; // Your implementation
            string accessToken = null; // Your access token

            if (api == null || string.IsNullOrWhiteSpace(accessToken))
            {
                Console.WriteLine("You need to set the api and the accessToken.");
                return;
            }

            var orders = new List<Order>();
            var startDate = DateTime.Now.AddDays(-30);
            if (api != null)
            {
                api.DeserializeAccessToken(accessToken);

                int numberOfPages;
                var currentPage = 1;
                const int pageSize = 50;
                do
                {
                    try
                    {
                        orders.AddRange(api.GetOrderList(
                            startDate,
                            19,
                            7,
                            out _,
                            out numberOfPages,
                            currentPage,
                            pageSize
                        ));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(
                            $"Execption while querying page {currentPage} with pageSize {pageSize} and startDate {startDate}");
                        Console.WriteLine(e);
                        throw;
                    }
                } while (++currentPage <= numberOfPages);
            }

            Console.WriteLine($"Loaded {orders.Count} order(s).");

            foreach (var order in orders)
            {
                var isValid = true;
                var uniqueIdentifier = string.IsNullOrWhiteSpace(order.Id) ? order.OrderNumber : order.Id;
                if (string.IsNullOrWhiteSpace(order.Id))
                {
                    isValid = false;
                    Console.WriteLine($"Order {uniqueIdentifier}: Missing {nameof(order.Id)}");
                }

                if (string.IsNullOrWhiteSpace(order.OrderNumber))
                {
                    isValid = false;
                    Console.WriteLine($"Order {uniqueIdentifier}: Missing {nameof(order.OrderNumber)}");
                }

                if (order.CreatedAt > startDate)
                {
                    isValid = false;
                    Console.WriteLine($"Order {uniqueIdentifier}: {nameof(order.CreatedAt)} was not set.");
                }

                if (string.IsNullOrWhiteSpace(order.Currency))
                {
                    isValid = false;
                    Console.WriteLine($"Order {uniqueIdentifier}: Missing {nameof(order.Currency)}");
                }
                else if (order.Currency.Trim().Length != 3)
                {
                    isValid = false;
                    Console.WriteLine(
                        $"Order {uniqueIdentifier}: {nameof(order.Currency)} must be a valid 3 letter currency code.");
                }

                if (order.InvoiceAddress == null)
                {
                    isValid = false;
                    Console.WriteLine($"Order {uniqueIdentifier}: Missing {nameof(order.InvoiceAddress)}");
                }
                else
                    isValid &= _validateAddress($"{uniqueIdentifier}.{nameof(order.InvoiceAddress)}",
                        order.InvoiceAddress);

                if (order.ShippingAddress == null)
                {
                    isValid = false;
                    Console.WriteLine($"Order {uniqueIdentifier}: Missing {nameof(order.ShippingAddress)}");
                }
                else
                    isValid &= _validateAddress($"{uniqueIdentifier}.{nameof(order.ShippingAddress)}",
                        order.ShippingAddress);

                if (isValid && string.IsNullOrWhiteSpace(order.ShippingAddress.Email)
                            && string.IsNullOrWhiteSpace(order.InvoiceAddress.Email))
                {
                    isValid = false;
                    Console.WriteLine(
                        $"Order {uniqueIdentifier}: Missing a Email in {nameof(order.InvoiceAddress)} or {nameof(order.ShippingAddress)}");
                }

                if (!string.IsNullOrWhiteSpace(order.IsCancellationFor) && !order.IsCanceled)
                {
                    isValid = false;
                    Console.WriteLine(
                        $"Order {uniqueIdentifier}: {nameof(order.IsCancellationFor)} is set but {nameof(order.IsCanceled)} is false.");
                }

                if (order.OrderItems == null)
                {
                    isValid = false;
                    Console.WriteLine($"Order {uniqueIdentifier}: Missing {nameof(order.OrderItems)}");
                }
                else if (!order.OrderItems.Any())
                {
                    isValid = false;
                    Console.WriteLine($"Order {uniqueIdentifier}: The order has no OrderItems and should be excluded.");
                }
                else _validateOrderItems(uniqueIdentifier, order);

                if (orders.Any(o => o.Id == order.Id && o.GetHashCode() != order.GetHashCode()))
                {
                    isValid = false;
                    Console.WriteLine($"Order {uniqueIdentifier}: There is more than one order with the same id.");
                }

                Console.WriteLine($"Order {uniqueIdentifier} is {(isValid ? "valid" : "invalid")}.");
                Console.WriteLine();
            }
        }

        private static void _validateOrderItems(string uniqueIdentifier, Order order)

        {
            var calculatedTotal = order.OrderItems.Sum(i => i.DiscountedPrice) + order.ShippingCost;
            if (order.TotalCost != calculatedTotal)
            {
                Console.WriteLine(
                    $"Order {uniqueIdentifier}: The TotalCost of {order.TotalCost} doesn't match the calculated total of {calculatedTotal}");
            }

            for (var index = 0; index < order.OrderItems.Count; index++)
            {
                var item = order.OrderItems[index];
                var identifier = $"{uniqueIdentifier}.{nameof(order.OrderItems)}[{index}]";
                if (item.Quantity == 0)
                {
                    Console.WriteLine($"Order {identifier}: {nameof(item.Quantity)} == 0");
                }

                if (item.TaxAmount != 0 && (item.TaxIndex == null || item.TaxIndex == 0))
                {
                    Console.WriteLine(
                        $"Order {identifier}: {nameof(item.TaxAmount)} != 0 but the {nameof(item.TaxIndex)} is not set.");
                }
                else if (item.TaxAmount == 0 && item.TaxIndex != null && item.TaxIndex != 0)
                {
                    Console.WriteLine(
                        $"Order {identifier}: {nameof(item.TaxAmount)} == 0 but the {nameof(item.TaxIndex)} is set to {item.TaxIndex}.");
                }
                else if (item.TaxAmount != 0)
                {
                    var vatRate = item.TaxIndex == 1 ? order.TaxRateRegular : order.TaxRateReduced;
                    var netPrice = (item.DiscountedPrice / (1 + (vatRate / 100))) ?? 0;
                    var calculatedTaxAmount =
                        Math.Round(item.DiscountedPrice - netPrice, 4, MidpointRounding.AwayFromZero);
                    if (item.TaxAmount != calculatedTaxAmount)
                    {
                        Console.WriteLine(
                            $"Order {identifier}: The {nameof(item.TaxAmount)} of {item.TaxAmount} doesn't match the calculated of {calculatedTaxAmount}.");
                    }
                }

                if (item.Product == null)
                {
                    Console.WriteLine($"Order {identifier}: Missing {nameof(item.Product)}");
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(item.Product.Title))
                    {
                        Console.WriteLine($"Order {identifier}.Product: Missing {nameof(item.Product.Title)}");
                    }

                    if (string.IsNullOrWhiteSpace(item.Product.SKU))
                    {
                        Console.WriteLine($"Order {identifier}.Product: Missing {nameof(item.Product.SKU)}");
                    }

                    if (string.IsNullOrWhiteSpace(item.Product.SKU))
                    {
                        Console.WriteLine($"Order {identifier}.Product: Missing {nameof(item.Product.SKU)}");
                    }
                }
            }
        }

        private static bool _validateAddress(string identifier, Address address)
        {
            var isValid = true;
            if (string.IsNullOrWhiteSpace(address.Company) &&
                (string.IsNullOrWhiteSpace(address.FirstName) || string.IsNullOrWhiteSpace(address.LastName))
            )
            {
                isValid = false;
                Console.WriteLine(
                    $"Order {identifier}: Missing {nameof(address.Company)} or {nameof(address.FirstName)} and {nameof(address.LastName)}");
            }

            if (string.IsNullOrWhiteSpace(address.Line2)
                && (string.IsNullOrWhiteSpace(address.Street) || string.IsNullOrWhiteSpace(address.HouseNumber))
            )
            {
                isValid = false;
                Console.WriteLine(
                    $"Order {identifier}: Missing {nameof(address.Street)} and {nameof(address.HouseNumber)} or {nameof(address.Line2)}"
                );
            }

            if (string.IsNullOrWhiteSpace(address.Zip))
            {
                isValid = false;
                Console.WriteLine($"Order {identifier}: Missing {nameof(address.Zip)}");
            }

            if (string.IsNullOrWhiteSpace(address.City))
            {
                isValid = false;
                Console.WriteLine($"Order {identifier}: Missing {nameof(address.City)}");
            }

            if (string.IsNullOrWhiteSpace(address.Country) && string.IsNullOrWhiteSpace(address.CountryISO2))
            {
                isValid = false;
                Console.WriteLine(
                    $"Order {identifier}: Missing {nameof(address.Country)} and {nameof(address.CountryISO2)}");
            }
            else if (!string.IsNullOrWhiteSpace(address.CountryISO2) && address.CountryISO2.Trim().Length != 2)
            {
                isValid = false;
                Console.WriteLine(
                    $"Order {identifier}: {nameof(address.CountryISO2)} must be a valid 2 letter country code");
            }

            return isValid;
        }
    }
}
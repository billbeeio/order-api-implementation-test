using System;
using System.Collections.Generic;
using System.Linq;

namespace Minimal_Order_Api
{
    public class Order
    {
        public string Id { get; set; }
        public string OrderNumber { get; set; }

        public OrderStateEnum State { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? ShippedAt { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        public DateTime? PayedAt { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public Address InvoiceAddress { get; set; }
        public Address ShippingAddress { get; set; }

        public decimal RebateDifference =>
            TotalCost - Math.Round(OrderItems.Sum(x => x.TotalPrice * ((100 - x.Discount) / 100M)), 4) - ShippingCost;

        public PaymentTypeEnum PaymentMethod { get; set; }

        public decimal? TaxRateRegular { get; set; }
        public decimal? TaxRateReduced { get; set; }

        /// <summary>Gross shipping cost</summary>
        public decimal ShippingCost { get; set; }

        /// <summary>Total cost for order including everything (shipping cost, taxes etc.)</summary>
        /// <remarks>
        /// This MUST NOT calculated on your own. In the most cases the API sends a total value which should be used.
        /// </remarks>
        public decimal TotalCost { get; set; }

        public string Currency { get; set; }
        public bool IsCanceled { get; set; }
        public List<OrderItem> OrderItems { get; set; }

        public string VatId { get; set; }

        public List<string> Tags { get; } = new List<string>();

        public string LanguageCode { get; set; }

        public decimal? PaidAmount { get; set; }
        public string ShippingProfileId { get; set; }
        public string ShippingProfileName { get; set; }

        /// <summary>
        /// An optional Order Id (externalid) for an order if this is a cancel order (shopify only at the moment)
        /// </summary>
        public string IsCancellationFor { get; set; }

        public string PaymentTransactionId { get; set; }

        public string PaymentReference { get; set; }
    }
    public class OrderItem
    {
        /// <summary>
        /// Id der Einzeltransaktion. Wird nur von Ebay benötigt, um zusammengefasste Bestellungen zu erkennen  OR  Id of the individual transaction. Only required by Ebay to detect aggregated orders
        /// </summary>
        public string TransactionId { get; set; }

        public SoldProduct Product { get; set; }
        public decimal Quantity { get; set; }

        /// <summary>
        /// gross price for the ordered <see cref="Quantity"/> including tax
        /// </summary>
        public decimal TotalPrice { get; set; }

        /// <summary>
        /// tax amount applied to this order item
        /// </summary>
        public decimal TaxAmount { get; set; }

        /// <summary>
        /// 0: tax free, 1: normal tax, 2: reduced tax
        /// </summary>
        public byte? TaxIndex { get; set; }

        public List<OrderItemAttribute> Attributes { get; set; }


        public bool IsCoupon { get; set; }

        public bool IsDiscount { get; set; }

        /// <summary>
        /// Sets the discount in percent
        /// </summary>
        public decimal Discount { get; set; }

        public decimal DiscountedPrice => Math.Round(Discount != 0 ? TotalPrice * (100 - Discount) / 100 : TotalPrice,
            2, MidpointRounding.AwayFromZero);

        public override string ToString()
        {
            return $"Q:{Quantity} TP:{TotalPrice} Tax:{TaxIndex} Discount:{Discount}";
        }
    }

    public class OrderItemAttribute
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class SoldProduct
    {
        public string Id { get; set; }
        public string Title { get; set; }

        public string SKU { get; set; }
    }

}
using System;
using System.Collections.Generic;
using System.Linq;

namespace Billbee.MinimalOrderApi
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
}
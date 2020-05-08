using System;
using System.Collections.Generic;

namespace Billbee.MinimalOrderApi
{
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
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EstyBudget.Entities
{
    public class SaleEntity
    {
        public string Description { get; set; }
        public string ItemType => "Etsy item";
        public DateTime Date { get; set; }
        public string BoughtOrSold => "Sold";
        public float? SellingPriceAndShipping {get; set; }
        public float? Expense { get; set; }
        public float? ShippingCost { get; set; }
        public float? EstyFees { get; set; }
        public float? CreditCardFees { get; set; }
        public float? PayPalFees { get; set; }
        public float? OtherFees { get; set; }
        public float? NetProfit { get; set; }
        public bool IsAdjustedSellingPriceAndShipping { get; set; }
    }
}

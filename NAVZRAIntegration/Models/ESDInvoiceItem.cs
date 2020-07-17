using System;
using System.Collections.Generic;
using System.Text;

namespace NAVZRAIntegration.Models
{
    class ESDInvoiceItem
    {
        public int ItemId { get; set; }
        public string Description { get; set; }
        public string Barcode { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Discount { get; set; }
        public char[] TaxLabels { get; set; }
        public decimal TotalAmount { get; set; }
        public bool isTaxInclusive { get; set; }
        public int RRP { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace NAVZRAIntegration.Models
{
    class ESDInvoiceHeader
    {
        public ESDInvoiceHeader()
        {
            Items = new HashSet<ESDInvoiceItem>();
        }

        public string POSSerialNumber { get; set; }
        public string PosVendor { get; set; }
        public string PosModel { get; set; }
        public string PosSoftVersion { get; set; }
        public string IssueTime { get; set; }
        public int TransactionType { get; set; }
        public int PaymentMode { get; set; }
        public int PaymentMethod { get; set; }
        public int SaleType { get; set; }
        public string LocalPurchaseOrder { get; set; }
        public string Cashier { get; set; }
        public string BuyerTPIN { get; set; }
        public string BuyerName { get; set; }
        public string BuyerTaxAccountName { get; set; }
        public string BuyerAddress { get; set; }
        public string BuyerTel { get; set; }
        public string OriginalInvoiceCode { get; set; }
        public string OriginalInvoiceNumber { get; set; }

        public virtual ICollection<ESDInvoiceItem> Items { get; set; }
    }
}

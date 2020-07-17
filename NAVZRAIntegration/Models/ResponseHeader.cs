using System;
using System.Collections.Generic;
using System.Text;

namespace NAVZRAIntegration.Models
{
    class ResponseHeader
    {
        public ResponseHeader()
        {
            TaxItems = new HashSet<ResponseLine>();
        }

        public string TPIN { get; set; }
        public string TaxpayerName { get; set; }
        public string Address { get; set; }
        public string ESDTime { get; set; }
        public string TerminalID { get; set; }
        public string InvoiceCode { get; set; }
        public string InvoiceNumber { get; set; }
        public string FiscalCode { get; set; }
        public string TalkTime { get; set; }
        public string Operator { get; set; }
        public string VerificationUrl { get; set; }
        public string VerificationQRCode { get; set; }
        public virtual ICollection<ResponseLine> TaxItems { get; set; }
    }
}

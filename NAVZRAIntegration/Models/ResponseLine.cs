using System;
using System.Collections.Generic;
using System.Text;

namespace NAVZRAIntegration.Models
{
    class ResponseLine
    {
        public string TaxLabel { get; set; }
        public string CategoryName { get; set; }
        public decimal Rate { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
    }
}

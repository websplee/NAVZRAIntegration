using System;
using System.Collections.Generic;
using System.Text;
using AutoMapper;
using NAVReadReference;
using NAVUpdateReference;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NAVZRAIntegration.Models;
using Newtonsoft.Json;
using System.Globalization;

namespace NAVZRAIntegration
{
    class NavInteraction
    {
        private SalesInvoiceIntegration_PortClient _navReadClient;
        private ZRASalesInvoice2_PortClient _navUpdateClient;
        private readonly IMapper _mapper;

        public NavInteraction(SalesInvoiceIntegration_PortClient navReadClient,
            ZRASalesInvoice2_PortClient navUpdateClient,
            IMapper mapper)
        {
            this._navReadClient = navReadClient;
            this._navUpdateClient = navUpdateClient;
            this._mapper = mapper;
        }

        // Read invoices from Nav
        public async Task<ZRASalesInvoice2[]> ReadInvoices()
        {
            var endPointResult = await this.TestEndPoint();
            if(endPointResult != "Success")
            {
                Console.WriteLine(endPointResult);
            }

            // ReadByRecId_Result recresult = await this._navUpdateClient.ReadByRecIdAsync("103001");
            // HEADER OR THE LINES?
            ZRASalesInvoice2_Filter[] filterArray = new ZRASalesInvoice2_Filter[1];
            ZRASalesInvoice2_Filter invoiceFilter = new ZRASalesInvoice2_Filter();
            invoiceFilter.Field = ZRASalesInvoice2_Fields.InvoiceSigned;
            invoiceFilter.Criteria = "No";
            filterArray[0] = invoiceFilter;
            ReadMultiple_Result recresult;
            
            // Attempt the read. Error handling done in the calling program class for managed logging
            
            recresult = await this._navUpdateClient.ReadMultipleAsync(filterArray, null, 100);
            
            // return the invoices
            return recresult.ReadMultiple_Result1;
        }

        // Update of returned invoice from ZRA into Nav
        public async void UpdateInvoices(string strInvoice, ZRASalesInvoice2 zRASalesInvoice)
        {
            //Console.WriteLine(strInvoice);
            //Console.ReadKey();
            // Convert string to json
            JObject jInvoice = JObject.Parse(strInvoice);

            ResponseHeader responseHeader = new ResponseHeader();

            responseHeader = JsonConvert.DeserializeObject<ResponseHeader>(strInvoice);
            // Fields to update
            // Ideally loop through items
            zRASalesInvoice.ESD_Time = DateTime.ParseExact(responseHeader.ESDTime, "yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            zRASalesInvoice.Fiscal_Code = responseHeader.FiscalCode;
            zRASalesInvoice.Terminal_ID = responseHeader.TerminalID;
            zRASalesInvoice.Invoice_Code = responseHeader.InvoiceCode;
            zRASalesInvoice.InvoiceSigned = true;
            // loop through the invoices
            var tmpSales = _mapper.Map<P_Sales_Inv_Line[]>(responseHeader.TaxItems);

            tmpSales.CopyTo(zRASalesInvoice.SalesInvLines, 0);

            NAVUpdateReference.Update navUpdate = new NAVUpdateReference.Update(zRASalesInvoice);
            
            // Attempt the actual update of the invoices. Errors are handled in the calling program
            Update_Result update_Result = await this._navUpdateClient.UpdateAsync(navUpdate);               
        }

        // Check if the Nav end point is online
        private async Task<string> TestEndPoint()
        {
            string msg;
            try
            {
                var read_Result = this._navUpdateClient.ReadAsync("1");
                if (read_Result.Status.Equals(TaskStatus.WaitingForActivation))
                    msg = "Offline";
                else
                    msg = "Success";
            }
            catch(Exception ex)
            {
                msg = ex.Message;
            }            
            return msg;
        }
    }
}

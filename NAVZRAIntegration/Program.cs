using System;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using NAVReadReference;
using NAVUpdateReference;
using NAVCreditMemoReference;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NAVZRAIntegration.Models;
using System.Threading.Tasks;
using System.Globalization;
using Serilog;
using Microsoft.Extensions.Configuration;

namespace NAVZRAIntegration
{
    class Program
    {
        static IMapper _mapper;
        static ServiceCollection _serviceCollection;
        static ZRASalesInvoice2_PortClient NavUpdateClient = new ZRASalesInvoice2_PortClient();
        static SalesInvoiceIntegration_PortClient NavReadClient = new SalesInvoiceIntegration_PortClient();
        static ZRASalesCreditMemo_PortClient NavCreditMemoClient = new zrasal

        static void Main(string[] args)
        {
            SetupStaticLogger();

            _serviceCollection = new ServiceCollection();
            ConfigureServices(_serviceCollection);

            try  // Try to read the invoices
            {
                ReadSendUpdateInvoices();
            }
            catch(Exception ex)
            {
                // throw ex;
                Log.Fatal(ex.Message);
            }
            finally
            {
                Log.CloseAndFlush();
            } // End try to read
            // Console.ReadKey();
        }

        // Services configuration
        private static void ConfigureServices(IServiceCollection services)
        {
            // Add Automapper
            // Automapper Configurations
            var mappingConfig = new  MapperConfiguration(mc =>
            {
                mc.AddProfile(new AutoMapperProfile());
            });
            _mapper = mappingConfig.CreateMapper();

            // Read invoices DI            
            NavReadClient.ClientCredentials.Windows.AllowedImpersonationLevel = System.Security.Principal.TokenImpersonationLevel.Impersonation;
            NavReadClient.ClientCredentials.Windows.ClientCredential = new System.Net.NetworkCredential("administrator", "NAV2009", "NAVISION");

            // Update invoices DI            
            NavUpdateClient.ClientCredentials.Windows.AllowedImpersonationLevel = System.Security.Principal.TokenImpersonationLevel.Impersonation;
            NavUpdateClient.ClientCredentials.Windows.ClientCredential = new System.Net.NetworkCredential("administrator", "NAV2009", "NAVISION");

            // Setup our DI
            services
                .AddSingleton(_mapper)
                .AddSingleton(NavReadClient)
                .AddSingleton(NavUpdateClient)                
                .AddLogging(configure => configure.SetMinimumLevel(LogLevel.Warning))
                .BuildServiceProvider();
        }

        // Setup Serilog Logging
        private static void SetupStaticLogger()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("SerilogConfig.json")
                .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();
        }

        private static async void ReadSendUpdateInvoices()
        {
            while (true)
            {
                // do the work in the loop
                ReadNavInvoices();

                // don't run again for at least 5 seconds
                await Task.Delay(5000);
            }
        }
        private static async  void ReadNavInvoices()
        {            
            NavInteraction nav = new NavInteraction(NavReadClient, NavUpdateClient, _mapper);
            ZRASalesInvoice2[] navInvoices;
            string zRAFeedback;

            try // Read NAv invoices
            {
                navInvoices = await nav.ReadInvoices();
                Log.Information("Number of records read " + navInvoices.Length);
            }
            catch (Exception ex)
            {
                // throw ex;
                Log.Error(ex.Message);
            }
            finally
            {
                navInvoices = null;
            } // End try Read NAv invoices

            ESDInvoiceHeader eSDInvoice = new ESDInvoiceHeader();

            // Check if invoices not null  
            if (navInvoices != null)
            {
                // For each invoice
                // 1. Append missing fields 
                foreach (var invoice in navInvoices)
                {
                    Log.Information(invoice.ToString());

                    // Map nav invoice to strongly typed invoice ZRA understandads
                    eSDInvoice = _mapper.Map<ESDInvoiceHeader>(invoice);


                    // Convert Issue date time to appropriate format
                    var issueDate = DateTime.Parse(eSDInvoice.IssueTime).ToString("yyyyMMddHHmmss");

                    // Insert temp tax labels                
                    foreach (var line in eSDInvoice.Items)
                    {
                        line.TaxLabels = new char[1];
                        line.TaxLabels[0] = 'A';
                        // THIS MUST BE REMOVED IN PRODUCTION
                        line.UnitPrice = Math.Round(line.UnitPrice, 2);
                        line.TotalAmount = Math.Round(line.Quantity * line.UnitPrice, 2);
                    }

                    // Assign POS Details to string and json
                    string strPOSDetails = "{\"POSSerialNumber\":\"100100003089\",\"PosVendor\":\"Inspur\",\"PosModel\":\"IS-100\",\"PosSoftVersion\":\"1.033-22\",\"Cashier\":\"MaryTest\",\"IssueTime\":\"" + issueDate + "\"}";
                    JObject jPOSDetails = JObject.Parse(strPOSDetails);


                    // Assign Invoice details to string and json
                    string strInvoice = JsonConvert.SerializeObject(eSDInvoice);
                    // Replace SalesInvLine with Items
                    // strInvoice = strInvoice.Replace("SalesInvLines", "Items");
                    // Convert jInvoice back to Json to remove IssueTime which is conflicting
                    JObject jInvoice = JObject.Parse(strInvoice);
                    jInvoice.Property("IssueTime").Remove();


                    // Merge POS Details and Invoice
                    JObject jZRAInvoice = jPOSDetails;
                    jZRAInvoice.Merge(jInvoice, new JsonMergeSettings
                    {
                        // union array values together to avoid duplicates
                        MergeArrayHandling = MergeArrayHandling.Union
                    });

                    string strZRAInvoice = JsonConvert.SerializeObject(jZRAInvoice);

                    try // Try send each invoice to ZRA
                    {
                        zRAFeedback = SendZRAInvoice(strZRAInvoice);

                        if (zRAFeedback.Contains("ErrorCode"))
                            throw new Exception(zRAFeedback);
                    }
                    catch (Exception ex)
                    {
                        // throw ex;
                        Log.Error(ex.Message);
                    }
                    finally
                    {
                        zRAFeedback = null;
                    } // End of send try

                    try // Try to update the invoices
                    {
                        // Update invoice
                        UpdateNavInvoice(zRAFeedback, invoice);
                        Log.Information("Success update " + invoice.ToString());
                    }
                    catch(Exception ex)
                    {
                        Log.Error("Failed to update " + ex.Message);
                    } // Try to update
                } // foreach
            } // if
        }
        // 2. Send to ZRA
        private static String SendZRAInvoice(string invoice)
        {
            var logger = _serviceCollection.BuildServiceProvider().GetService<ILogger<ZRAInteration>>();
            ZRAInteration zRA = new ZRAInteration(_mapper, logger);

            return zRA.PrepareZRAESDData(invoice);

        }

        // 3. Get feedback from ZRA and update invoice
        private  static void UpdateNavInvoice(string strInvoice, ZRASalesInvoice2 invoice)
        {
            var logger = _serviceCollection.BuildServiceProvider().GetService<ILogger<NavInteraction>>();
            NavInteraction nav = new NavInteraction(NavReadClient, NavUpdateClient, _mapper);
            nav.UpdateInvoices(strInvoice, invoice);
        }
           

    }
}

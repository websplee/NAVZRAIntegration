using AutoMapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NAVZRAIntegration
{
    class ZRAInteration
    {
        private readonly IMapper _mapper;
        private readonly ILogger _logger;

        public ZRAInteration(IMapper mapper, ILogger<ZRAInteration> logger)
        {
            this._mapper = mapper;
            this._logger = logger;
        }
        public String PrepareZRAESDData(string content)
        {
            byte header1 = 0x1A;
            byte header2 = 0x5D;
            byte statusCMD = 0x02;
            //byte signCMD = 0x02;
            //byte errorCMD = 0x03;
            byte[] header = new byte[] { header1, header2, statusCMD };

            //Console.WriteLine(Encoding.ASCII.GetString(new byte[] { signCMD, header2 }));
            // String content = "{\"PosSerialNumber\":\"100100003089\",\"PosVendor\":\"Inspur\",\"PosModel\":\"IS-100\",\"PosSoftVersion\":\"1.033-22\"}";
            // content = "{\"POSSerialNumber\":\"100100003089\",\"PosVendor\":\"Inspur\",\"PosModel\":\"IS-100\",\"PosSoftVersion\":\"1.033-22\",\"IssueTime\":\"20050114091840\",\"TransactionType\":0,\"PaymentMode\":0,\"PaymentMethod\":0,\"SaleType\":0,\"LocalPurchaseOrder\":\"CA3300440024488\",\"Cashier\":\"20\",\"BuyerTPIN\":\"\",\"BuyerName\":\"\",\"BuyerTaxAccountName\":\"\",\"BuyerAddress\":\"\",\"BuyerTel\":\"\",\"OriginalInvoiceCode\":\"\",\"OriginalInvoiceNumber\":\"\",\"Items\":[{\"ItemId\":1,\"Description\":\"5-PKT TURNUP JEAN\",\"Barcode\":\"\",\"Quantity\":1,\"UnitPrice\":55.00,\"Discount\":0.00,\"TaxLabels\":[\"A\"],\"TotalAmount\":55.00,\"isTaxInclusive\":true,\"RRP\":0} ] }";
            
            byte[] contentBytes = Encoding.ASCII.GetBytes(content);
            byte[] contentLength = new byte[4];
            byte[] cntLength = BitConverter.GetBytes(contentBytes.Length);
            for (int i = 0; i < cntLength.Length; i++)
            {
                contentLength[i] = cntLength[i];
            }
            Array.Reverse(contentLength);

            byte[] data = new byte[header.Length + contentLength.Length + contentBytes.Length];
            Array.Copy(header, 0, data, 0, header.Length);
            Array.Copy(contentLength, 0, data, header.Length, contentLength.Length);
            Array.Copy(contentBytes, 0, data, (header.Length + contentLength.Length), contentBytes.Length);

            //Console.WriteLine(ByteArrayToString(data));
            //byte[] crc = BitConverter.GetBytes(CalCRC(data, data.Length));
            int bresult;
            unsafe void GetBytePtr()
            {

                fixed (byte* p = data)
                {
                    bresult = CalCRC(p, data.Length);
                }
            }
            GetBytePtr();
            byte[] tmp = BitConverter.GetBytes(bresult);// new byte[2] { 0x89, 0x3e}; //            BitConverter.GetBytes(Crc16.ComputeChecksum(data));
            byte[] crc = new byte[] { tmp[1], tmp[0] };

            Console.WriteLine(ByteArrayToString(crc));

            //StartListening(); 
            var test = data.Concat(crc).ToArray();
            Console.WriteLine(ByteArrayToString(test));
            return Send(test);
            //Console.WriteLine(Send(test));
            //Console.WriteLine(Send(Encoding.ASCII.GetBytes("1A5D01000000337B22506F7353657269616C4E756D626572223A223132333435363738222C22506F7356656E646F72223A22496E73707572227D893E")));
            // Console.ReadKey();

            //ComPort.Write(test);
        }

        private string ConvertHex(String hexString)
        {
            /*
            try
            {
                string ascii = string.Empty;

                for (int i = 0; i < hexString.Length; i += 2)
                {
                    String hs = string.Empty;

                    hs = hexString.Substring(i, 2);
                    uint decval = System.Convert.ToUInt32(hs, 16);
                    char character = System.Convert.ToChar(decval);
                    ascii += character;

                }

                return ascii;
            }
            catch (Exception ex) { Console.WriteLine(ex.StackTrace); }

            return string.Empty;
            */
            byte[] tmp;
            int j = 0;
            tmp = new byte[(hexString.Length) / 2];
            for (int i = 0; i <= (hexString.Length - 2); i += 2)
            {
                tmp[j] = (byte)Convert.ToChar(Int32.Parse(hexString.Substring(i, 2), System.Globalization.NumberStyles.HexNumber));

                j++;
            }
            return Encoding.GetEncoding(1252).GetString(tmp);
        }

        private String Send(byte[] hexData)
        {
            byte[] data = hexData; // Encoding.ASCII.GetBytes(ConvertHex(Encoding.ASCII.GetString(hexData)));
            //Console.WriteLine(ConvertHex(Encoding.ASCII.GetString(hexData)));

            String response = null;
            try
            {
                try
                {
                    TcpClient client = new TcpClient();
                    client.Connect(IPAddress.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("ESDIPAddress")),
                                   Int32.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("ESDPort")));
                    client.GetStream().Write(data, 0, data.Length);
                    Console.WriteLine("Data sent");

                    byte[] receiveData = new byte[100024];
                    String responseData = String.Empty;
                    Int32 bytes = client.GetStream().Read(receiveData, 0, receiveData.Length);
                    response = Encoding.ASCII.GetString(receiveData, 0, bytes);
                    Console.WriteLine(response);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unexpected exception: {0}", e.ToString());
                    return e.Message;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return response.Substring(response.IndexOf('{')) + "\"}";
            
        }

        private String ByteArrayToString(byte[] data)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in data)
            {
                sb.AppendFormat(String.Format("{0:X2} ", b));
            }
            return sb.ToString();
        }


        private unsafe Int16 CalCRC(byte* ptr, int len)
        {
            byte i;
            uint crc = 0;

            while (len-- != 0)
            {
                for (i = 0x80; i != 0; i /= 2)
                {
                    if ((crc & 0x8000) != 0)
                    {
                        crc *= 2;
                        crc ^= 0x18005;
                    }
                    else
                    {
                        crc *= 2;
                    }
                    if ((*ptr & i) != 0)
                        crc ^= 0x18005;
                }
                ptr++;
            }
            return (Int16)(crc);
            /*
            byte i;
            int crc = 0;
            while (len-- != 0)
            {
                for (i = 0x80; i != 0; i /= 2)
                {
                    if ((crc & 0x8000) != 0)
                    {
                        crc *= 2;
                        crc ^= 0x18005;
                    }
                    else
                    {
                        crc *= 2;
                    }
                    if ((ptr[len] & i) != 0)
                        crc ^= 0x18005;
                }
            }
            return (crc);*/
        }

        private void StartListening()
        {
            // Data buffer for incoming data.  
            byte[] bytes = new Byte[1024];

            String data = "";

            // Establish the local endpoint for the socket.  
            // Dns.GetHostName returns the name of the   
            // host running the application.  
            IPAddress ipAddress = IPAddress.Parse("192.168.8.100");
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 8888);

            // Create a TCP/IP socket.  
            Socket listener = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);

            // Bind the socket to the local endpoint and   
            // listen for incoming connections.  
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(10);

                // Start listening for connections.  
                while (true)
                {
                    Console.WriteLine("Waiting for a connection...");
                    // Program is suspended while waiting for an incoming connection.  
                    Socket handler = listener.Accept();
                    data = "";

                    // An incoming connection needs to be processed.  
                    // while (true)
                    // {
                    int bytesRec = handler.Receive(bytes);
                    data += Encoding.ASCII.GetString(bytes, 0, bytesRec);
                    Console.WriteLine(data);

                    handler.Send(Encoding.ASCII.GetBytes("Received!....."));
                    /*if (data.IndexOf("<EOF>") > -1)
                    {
                        break;
                    }*/
                    //}

                    // Show the data on the console.  
                    Console.WriteLine("Text received : {0}", data);

                    // Echo the data back to the client.  
                    byte[] msg = Encoding.ASCII.GetBytes(data);

                    handler.Send(msg);
                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("\nPress ENTER to continue...");
            Console.Read();

        }
    }
}

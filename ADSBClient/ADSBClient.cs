using Newtonsoft.Json;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LicenseContext = OfficeOpenXml.LicenseContext;

namespace ADSBClientLib
{
    public class ADSBClient
    {
        public event EventHandler OnConnect;
        public event EventHandler OnDisconnect;
        public event EventHandler<string> OnReadByte;
        public event EventHandler<MyEventArgsADSBData> OnUpdateADSBData;

        ADSBParser parser = new ADSBParser();

        public static LocalProperties localProperties;
        public static Yaml yaml = new Yaml();

        TcpClient tcpClient;
        NetworkStream streamClient;

        SerialPort serialPort;

        public ADSBClient()
        {
            localProperties = yaml.YamlLoad();
            localProperties.IsSend2UDPAdsbReceiver = false;
            //CreateExcel("F:\\АДСБ для теста\\document.xlsx");
            CreateExcel(Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.Parent.FullName + "\\planesInfo.xlsx");
        }
        public ADSBClient(bool isSendToUDPClient)
        {
            localProperties = yaml.YamlLoad();

            localProperties.IsSend2UDPAdsbReceiver = isSendToUDPClient;

            //CreateExcel("F:\\АДСБ для теста\\document.xlsx");
            CreateExcel(Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.Parent.FullName + "\\planesInfo.xlsx");
        }
        public ADSBClient(bool isSendToUDPClient, string udpClientIp, int udpClientPort)
        {
            localProperties = yaml.YamlLoad();

            localProperties.IsSend2UDPAdsbReceiver = isSendToUDPClient;
            localProperties.UDPAdsbReceiverIp = udpClientIp;
            localProperties.UDPAdsbReceiverPort = udpClientPort;

            //CreateExcel("F:\\АДСБ для теста\\document.xlsx");
            CreateExcel(Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.Parent.FullName + "\\planesInfo.xlsx");
        }


        CancellationTokenSource cancelTokenSource = new CancellationTokenSource();
        CancellationToken token;
        /// <summary>
        /// for TCP connection
        /// </summary>
        /// <param name="strConnectionType"></param>
        /// <param name="serialPortName"></param>
        /// <param name="serialPortBaudRate"></param>
        /// <returns></returns>
        public bool Connect()
        {
            token = cancelTokenSource.Token;
            if (tcpClient != null && tcpClient.Connected == true)
            {
                tcpClient.Close();
            }
            tcpClient = new TcpClient();

            try
            {

                // create client for receiving raw hex data from Bullion
                tcpClient = new System.Net.Sockets.TcpClient();

                // begin connect 
                var result = tcpClient.BeginConnect("127.0.0.1", 30005, null, null);
                //var result = tcpClient.BeginConnect("192.168.0.11", 30005, null, null);
                //var result = tcpClient.BeginConnect(localProperties.IPtcpMicroAdsb, localProperties.TcpPortMicroAdsb, null, null);

                // try connect
                var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(5));

                if (!success)
                {
                    //throw new Exception("Failed to connect.");

                    OnDisconnect?.Invoke(this, EventArgs.Empty);
                    return false;
                }

                // we have connected
                tcpClient.EndConnect(result);
            }
            catch (System.Exception ex)
            {
                OnDisconnect?.Invoke(this, EventArgs.Empty);
                return false;
            }

            if (tcpClient.Connected)
            {
                OnConnect?.Invoke(this, EventArgs.Empty);
                try
                {

                    streamClient = tcpClient.GetStream();
                      
                    ReadDataNoReadThr(streamClient);
                }
                catch (Exception ex)
                {
                    // generate event
                    //OnDisconnect?.Invoke(this, EventArgs.Empty);
                    //return false;
                }
            }
            return true;            
        }
       
        /// <summary>
        /// for COM port connection
        /// </summary>
        /// <param name="serialPortName"></param>
        /// <param name="serialPortBaudRate"></param>
        /// <returns></returns>
        public bool Connect(string serialPortName, int serialPortBaudRate)
        {
            try
            {
                serialPort = new SerialPort(serialPortName, serialPortBaudRate);

                // Set other properties of the port
                serialPort.Parity = Parity.None;
                serialPort.DataBits = 8;
                serialPort.StopBits = StopBits.One;
                serialPort.Handshake = Handshake.None;
                // Open the port
                serialPort.Open();

                if (serialPort.IsOpen == true)
                {
                    OnConnect?.Invoke(this, EventArgs.Empty);
                    Task.Run(()=>ReadDataNoReadThr(serialPort));
                    //ReadDataNoReadThrTest(serialPort);//!!!!!!!!!!!!!!for test
                }
                return true;
            }
            catch(Exception ex)
            {
                OnDisconnect?.Invoke(this, EventArgs.Empty);
            }
          
            return false;
        }

        public bool Disconnect()
        {
            if (serialPort != null)
            {
                try
                {
                    
                    serialPort.Close();
                }
                catch (Exception ex)
                {
                    return false;
                }

            }
            // if there is client
            if (streamClient != null)
            {
                // close client stream
                try
                {
                    streamClient.Close();
                }
                catch (System.Exception)
                {
                    return false;
                }
            }
            // if there is client
            if (tcpClient != null)
            {
                // close client connection
                try
                {
                    tcpClient.Close();
                    tcpClient = null;
                }
                catch (System.Exception)
                {
                    return false;
                }
            }
            // generate event
            OnDisconnect?.Invoke(this, EventArgs.Empty);
            return true;
        }

        bool _continueTCP = false;
        /// <summary>
        /// read TCP adsb Bullion connection
        /// </summary>
        /// <param name="streamClient"></param>
        private void ReadDataNoReadThr(NetworkStream streamClient)
        {
            //exitcounter = 0;
            _continueTCP = true;//added 03122020 for M2 version
            byte[] bBufRead = new byte[1000];
            byte[] bBufSave = new byte[1000];

            int iReadByte = -1;
            string strPackage = "";

            while (_continueTCP)
            {
                try
                {
                    Array.Clear(bBufRead, 0, bBufRead.Length);

                    iReadByte = streamClient.Read(bBufRead, 0, bBufRead.Length);
                    //Console.WriteLine(iReadByte);
                    //exitcounter += 1; 
                    if (iReadByte >= 1)
                    {
                        //if (OnReadByte != null)
                        //    OnReadByte(this, strPackage);

                        Array.Resize(ref bBufRead, iReadByte);


                        try
                        {
                            string newstr = BitConverter.ToString(bBufRead);
                            newstr = newstr.Replace("-", string.Empty);
                            strPackage += newstr;                            
                            if (strPackage.Length >= 84) 
                            {
                                currentTime = DateTime.Now;
                                bool isParsed = ParseBullionData(strPackage); 
                                strPackage = "";
                                strPackage += remainder;

                            }

                        }
                        catch (Exception ex)
                        {

                        }


                    }

                }

                catch (System.Exception ex)
                {
                    _continueTCP = false;
                    OnDisconnect?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// read serial port(COM port) connection (from Alexei's tester made adsb)
        /// </summary>
        /// <param name="streamClient"></param>
        private void ReadDataNoReadThrTest(SerialPort serialPort)
        {
            //exitcounter = 0;
            bool _continue = true;//added 03122020 for M2 version
            byte[] bBufRead = new byte[1000];
            byte[] bBufSave = new byte[1000];

            int iReadByte = -1;
            string strPackage = "";
            while (_continue)
            {
                try
                {
                    Array.Clear(bBufRead, 0, bBufRead.Length);

                    iReadByte = serialPort.Read(bBufRead, 0, bBufRead.Length);
                    //Console.WriteLine(iReadByte);
                    //exitcounter += 1; 
                    if (iReadByte >= 1)
                    {
                        //if (OnReadByte != null)
                        //    OnReadByte(this, strPackage);

                        Array.Resize(ref bBufRead, iReadByte);


                        try
                        {
                            string newstr = BitConverter.ToString(bBufRead);
                            newstr = newstr.Replace("-", string.Empty);
                            strPackage += newstr;
                            if (strPackage.Length >= 84)
                            {
                                currentTime = DateTime.Now;
                                bool isParsed = ParseBullionData(strPackage);
                                strPackage = "";
                                strPackage += remainder;

                            }

                        }
                        catch (Exception ex)
                        {

                        }


                    }

                }

                catch (System.Exception ex)
                {
                    _continue = false;
                    OnDisconnect?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        bool _continueCOM = false;
        /// <summary>
        /// read serial port(COM port) connection (from Dima made adsb)
        /// </summary>
        /// <param name="streamClient"></param>
        private void ReadDataNoReadThr(SerialPort serialPort)
        {            
            _continueCOM = true;
            byte[] bBufRead = new byte[1000];
            byte[] bBufSave = new byte[1000];

            int iReadByte = -1;
            string strPackage = "";
            while (_continueCOM)
            {
                try
                {
                    Array.Clear(bBufRead, 0, bBufRead.Length);

                    iReadByte = serialPort.Read(bBufRead, 0, bBufRead.Length);
                    //Console.WriteLine(iReadByte);
                    //exitcounter += 1; 
                    if (iReadByte >= 1)
                    {
                        //if (OnReadByte != null)
                        //    OnReadByte(this, strPackage);

                        Array.Resize(ref bBufRead, iReadByte);

                        
                        try
                        {
                            string newstr = BitConverter.ToString(bBufRead);                            
                            newstr = newstr.Replace("-", string.Empty);
                            strPackage += newstr;
                            if (strPackage.Length == 7)
                            {
                                currentTime = DateTime.Now;
                                bool isParsed = ParseDimaData(strPackage);
                                if (isParsed)
                                {
                                    strPackage = "";
                                }

                            }
                            if (strPackage.Length == 14)
                            {                                
                                currentTime = DateTime.Now;
                                bool isParsed = ParseDimaData(strPackage);
                                if (isParsed)
                                {
                                    strPackage = "";
                                }
                                else 
                                { 
                                    strPackage = newstr;
                                    currentTime = DateTime.Now;
                                    isParsed = ParseDimaData(strPackage);
                                    strPackage = "";
                                }
                            }


                        }
                        catch (Exception ex)
                        {

                        }


                    }

                }

                catch (System.Exception ex)
                {
                    _continueCOM = false;
                    OnDisconnect?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Parsing of data packets  from emulator
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        ///       
        public static DateTime previouseTime = new DateTime();
        public static DateTime currentTime = new DateTime();// time of packet was received from adsb
        public static TimeSpan packetInterval = new TimeSpan();
        public static int packetcounter = 0;
        public static double sum = 0;
        public static double averageTime = 0;
        public static System.Net.Sockets.UdpClient udpPacketSender = new System.Net.Sockets.UdpClient();
        public static IPEndPoint endPointClientUdp;


        bool isParsed = false;
        public static string remainder = "";//end remainder of the parsed string (probably real message that started with 2A but hasnt ended with end of message - 3B)
        
        
        //messages should be selected handly from the stream of bytes from Bullion adsb
        private bool ParseBullionData(string str)
        {
            try
            {
                remainder = "";
                for (int i = 0; i < str.Length; i++)
                {
                    //finding the start of the message
                    if (str[i].Equals('2') && str[i + 1].Equals('A'))
                    {
                        remainder = remainder + str[i] + str[i+1];
                        i += 2;
                        int j = 0;
                        bool isoutofstring = false;

                        //finding end of the message
                        while (!(str[i + j].Equals('3') && str[i + j + 1].Equals('B')))
                        {                            
                            remainder = remainder + str[i + j];
                            j++;                            
                            if (j > 56) 
                            { 
                                isoutofstring = true; remainder = ""; break; 
                            }
                            if (i + j + 1>= str.Length)
                            {              
                                remainder += str[i + j];
                                isoutofstring = true;                               
                                break;
                            }
                        }

                        if (j >= 28 && j <= 56 && isoutofstring == false)
                        {
                            remainder = "";
                            string msg = "";
                            for (int k = i; k < i + j; k++)
                            {
                                msg = msg + str[k];//collect just message body in ascii of the adsb packet(just hex bytes between bytes 2a = * and 3b = ; ) and then parse this body depending on first DF byte
                            }
                            isParsed = parser.ParseData(msg, currentTime);
                            if (isParsed == true)
                            {
                                OnUpdateADSBData?.Invoke(this, new MyEventArgsADSBData(parser.planeData));
                                //Console.WriteLine("data is parsed. ICAO: " + parser.planeData.Icao);

                                if (localProperties.IsWriteDataToExcel == true) 
                                {
                                    WriteToExcel(parser.planeData);
                                }


                                //+ " DF: " +parser.listPlanes[parser.listPlanes.Count-1].DF); }
                                try
                                {

                                    if (localProperties.IsSend2UDPAdsbReceiver == true)
                                    {
                                        ///client for getting AeroScope packets by udp               
                                        udpPacketSender = new System.Net.Sockets.UdpClient();
                                        endPointClientUdp = new IPEndPoint(IPAddress.Parse(localProperties.UDPAdsbReceiverIp), localProperties.UDPAdsbReceiverPort);
                                        string json = JsonConvert.SerializeObject(parser.listPlanes);
                                        udpPacketSender.Send(Encoding.UTF8.GetBytes(json), Encoding.UTF8.GetBytes(json).Length, endPointClientUdp);
                                        udpPacketSender.Close();
                                        Console.WriteLine("UDP packet was sent to" + " IP: " + localProperties.UDPAdsbReceiverIp + " Port: " + localProperties.UDPAdsbReceiverPort);
                                    }

                                }
                                catch (Exception ex)
                                {

                                }
                                i += j;
                                //return true;
                            }
                            
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return false;
            }
            return false;
        }

        //messages are sent via comport one by one
        private bool ParseDimaData(string str)
        {
            try
            {
                isParsed = parser.ParseDimaData(str, currentTime);
                if (isParsed == true)
                {
                    OnUpdateADSBData?.Invoke(this, new MyEventArgsADSBData(parser.planeData));
                    //Console.WriteLine("data is parsed. ICAO: " + parser.planeData.Icao);

                    if (localProperties.IsWriteDataToExcel == true)
                    {
                        WriteToExcel(parser.planeData);
                    }


                    //+ " DF: " +parser.listPlanes[parser.listPlanes.Count-1].DF); }
                    try
                    {

                        if (localProperties.IsSend2UDPAdsbReceiver == true)
                        {
                            ///client for getting AeroScope packets by udp               
                            udpPacketSender = new System.Net.Sockets.UdpClient();
                            endPointClientUdp = new IPEndPoint(IPAddress.Parse(localProperties.UDPAdsbReceiverIp), localProperties.UDPAdsbReceiverPort);
                            string json = JsonConvert.SerializeObject(parser.listPlanes);
                            udpPacketSender.Send(Encoding.UTF8.GetBytes(json), Encoding.UTF8.GetBytes(json).Length, endPointClientUdp);
                            udpPacketSender.Close();
                            Console.WriteLine("UDP packet was sent to" + " IP: " + localProperties.UDPAdsbReceiverIp + " Port: " + localProperties.UDPAdsbReceiverPort);
                        }

                    }
                    catch (Exception ex)
                    {

                    }
                               
                    //return true;
                }

                    
            }
            catch (Exception ex)
            {
                return false;
            }
            return false;
        }

        string excelFilePath;
        /// <summary>
        /// create or open existing excel file for adsb data writing with specified columns 
        /// </summary>
        /// <param name="filePath"></param>
        private void CreateExcel(string filePath)
        {
            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                // Specify the file path for the Excel document
                excelFilePath = filePath;
                FileInfo fileInfo = new FileInfo(excelFilePath);                

                // Create a single-dimensional array of strings
                var data = new string[]
                {
                  "ICAO", "Model", "Latitude", "Longitude", "Track angle", "Altitude", "PacketTime", "Country"
                };

                // Wrap the array of objects in a list of object arrays
                var wrapped = new List<object[]> { data };
                // Create a new Excel package
                using (var package = new ExcelPackage(fileInfo))
                {
                    var sheet = package.Workbook.Worksheets["Data"];
                    if (sheet == null)
                    {
                        // Access the worksheet named "Data"
                        sheet = package.Workbook.Worksheets.Add("Data");
                    }

                    // Append the data to the worksheet starting from the next row
                    sheet.Cells[ 1, 1].LoadFromArrays(wrapped);

                    // Save the file
                    package.Save();
                }
            }
            catch (Exception ex)
            {

            }
        }
        private void WriteToExcel(PlaneInfoLib.PlaneData planeData)
        {
            try
            {                
                // Specify the file path for the Excel document
                FileInfo fileInfo = new FileInfo(excelFilePath);

                // Create a single-dimensional array of strings
                var data = new string[]
                {
                    planeData.Icao, planeData.ModelName, planeData.Lat.ToString(), planeData.Long.ToString(), planeData.TrackAngle.ToString(), planeData.Altitude.ToString(), planeData.PackageReceiveTime.ToLongDateString() + " " + planeData.PackageReceiveTime.ToShortTimeString(), planeData.Country.ToString()
                };

                // Wrap the array of objects in a list of object arrays
                var wrapped = new List<object[]> { data };
                // Create a new Excel package
                using (var package = new ExcelPackage(fileInfo))
                {
                    // Access the worksheet named "Data"
                    var sheet = package.Workbook.Worksheets["Data"];

                    // Get the address of the range that has data
                    var address = sheet.Dimension;

                    // Get the row index of the last cell that has data
                    var lastRow = address.End.Row;

                    // Append the data to the worksheet starting from the next row
                    sheet.Cells[lastRow + 1, 1].LoadFromArrays(wrapped);

                    // Save the file
                    package.Save();
                }
            }
            catch(Exception ex)
            {

            }
        }
    }
}







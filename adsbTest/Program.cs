using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace adsbTest
{
    class Program
    {
        public static LocalProperties localProperties;
        public static Yaml yaml = new Yaml();
        static void Main(string[] args)
        {

            TcpClient tcpClient = new TcpClient();
            NetworkStream streamClient;

            try
            {              
                // create client
                tcpClient = new System.Net.Sockets.TcpClient();

                // begin connect 
                var result = tcpClient.BeginConnect("127.0.0.1", 30005, null, null);

                // try connect
                var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(5));

                if (!success)
                {
                    throw new Exception("Failed to connect.");
                }
                                                   
                // we have connected
                tcpClient.EndConnect(result);
            }
            catch (System.Exception ex)
            {
                //generate event 
                //OnDisconnect?.Invoke(this, EventArgs.Empty);
                //return false;
            }

            if (tcpClient.Connected)
            {
                try   
                {
                    streamClient = tcpClient.GetStream();
                    //no read thread. Jusr waiting aeroscope data in readData function.
                    ReadDataNoReadThr(streamClient);                   
                }
                catch (Exception ex)
                {
                    // generate event
                    //OnDisconnect?.Invoke(this, EventArgs.Empty);
                    //return false;
                }
            }

        }
        
        public static void ReadDataNoReadThr(NetworkStream streamClient)
        {
            //exitcounter = 0;
            bool _continue = true;//added 03122020 for M2 version
            byte[] bBufRead = new byte[1000];
            byte[] bBufSave = new byte[1000];

            int iReadByte = -1;
            string strPackage="";
            while (_continue)
            {
                try
                {
                    Array.Clear(bBufRead, 0, bBufRead.Length);

                    iReadByte =  streamClient.Read(bBufRead, 0, bBufRead.Length);
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
                                strPackage += newstr;                                
                                if (strPackage.Length >= 200) { strPackage = strPackage.Replace("-", string.Empty); bool result = Parse1(strPackage); strPackage = ""; }
                           
                            }
                            catch (Exception ex)
                            {

                            }
                        
                     
                    }

                }

                catch (System.Exception ex)
                {
                    _continue = false;               
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
        public static DateTime currentTime = new DateTime();
        public static TimeSpan packetInterval = new TimeSpan();
        public static int packetcounter = 0;
        public static double sum = 0;
        public static double averageTime = 0;
        public static System.Net.Sockets.UdpClient udpPacketSender = new System.Net.Sockets.UdpClient();
        public static IPEndPoint endPointClientUdp;
        private static bool Parse1(string str)
        {
            try
            {
                AdsbParser parser = new AdsbParser();
                for (int i = 0; i < str.Length; i++)
                {
                    //finding the start of the message
                    if (str[i].Equals('2') && str[i + 1].Equals('A'))
                    {
                        i += 2;
                        int j = 0;
                        bool isoutofstring = false;

                        //finding end of the message
                        while (!(str[i+j].Equals('3') && str[i+j+1].Equals('B')))
                        {
                            j++;                         
                            if (j > 30 || i+j>str.Length) { isoutofstring = true; break; }
                        }
                        if (j>=10 && j<= 30 && isoutofstring == false)
                        {
                            bool isParsed = parser.ParseData(String.Concat(str[i], str[i + 1], str[i + 2], str[i + 3], str[i + 4], str[i + 5], str[i + 6], str[i + 7], str[i + 8], str[i + 9], str[i + 10], str[i + 11], str[i + 12], str[i + 13], str[i + 14], str[i + 15], str[i + 16], str[i + 17], str[i+18], str[i+19], str[i + 20], str[i + 21]));
                            if (isParsed == true) { Console.WriteLine("data is parsed. ICAO: " + parser.listPlanes[parser.listPlanes.Count-1].Icao + "DF: " +parser.listPlanes[parser.listPlanes.Count-1].DF); }
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
                        }                        
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }     


    }
}

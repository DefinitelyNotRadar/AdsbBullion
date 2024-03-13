using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ADSBClientLib;

namespace ADSBClientTest
{
    class Program
    {
        public static ADSBClient client;
        static void Main(string[] args)
        {
            try
            {
                ADSBClient adsbClient = new ADSBClient();//распаршенные данные никуда(в смысле на udp клиента) отсылаться не будут
                //ADSBClient adsbClient = new ADSBClient(true, "127.0.0.1", 16002);
                //ADSBClient adsbClient = new ADSBClient(true, "192.168.0.11", 16002);//распаршенные данные будут отправляться по udp на указанный в скобках адрес и порт 
                adsbClient.OnUpdateADSBData += AdsbClient_OnUpdateADSBData;
                adsbClient.OnConnect += AdsbClient_OnConnectADSB;
                adsbClient.OnDisconnect += AdsbClient_OnDisconnectADSB;
                adsbClient.Connect();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static void AdsbClient_OnUpdateADSBData(object sender, MyEventArgsADSBData e)
        {
            Console.WriteLine("Icao: " + e.planeData.Icao + " AirId: " + e.planeData.AirId + " Lat: " + e.planeData.Lat + " Long: " + e.planeData.Long + " Height: " + e.planeData.Altitude + " Velocity: " + e.planeData.AirSpeed + " AirCat: " + e.planeData.AirCat + " Heading: " + e.planeData.TrackAngle + " Ground speed: " + e.planeData.GroundSpeed + " Altitude Baro: " + e.planeData.GnssBaroHeightDelta); 
        }
        private static void AdsbClient_OnConnectADSB(object sender, EventArgs e)
        {
            Console.WriteLine("Соединение установлено.");
        }
        private static void AdsbClient_OnDisconnectADSB(object sender, EventArgs e)
        {
            Console.WriteLine("Соединение разорвано!");
            Console.ReadLine();
        }
    }
}

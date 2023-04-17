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
                ADSBClient adsbClient = new ADSBClient(true, "127.0.0.1", 16002);
                adsbClient.OnUpdateADSBData += AdsbClient_OnUpdateADSBData;
                adsbClient.Connect();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static void AdsbClient_OnUpdateADSBData(object sender, MyEventArgsADSBData e)
        {
            Console.WriteLine(e.planeData.Icao);
        }
    }
}

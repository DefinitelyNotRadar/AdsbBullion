using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UDPAdsbReceiver;

namespace ConsoleApp1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            Client receiver = new Client(16002);
            receiver.OnUDPPacketReceived += processPacket;
            try
            {
                receiver.Listen();
            }
            catch (Exception ex)
            {

            }
            Console.ReadLine();
        }
        private static void processPacket(object sender, List<PlaneData> e)
        {
            //вывод данных e.tableAeroscope
            Console.WriteLine("Data updated! Icao: " + e.Last().Icao + " Lat: " + e.Last().Lat + " Long: " + e.Last().Long);            
            Console.WriteLine("---------------------------------------------------");

        }
    }
}

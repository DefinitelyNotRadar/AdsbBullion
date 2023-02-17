using PlaneInfoLib;
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
            receiver.OnUDPPacketReceived += ProcessPacket;
            try
            {
                receiver.Listen();
            }
            catch (Exception ex)
            {

            }
            Console.ReadLine();
        }
        private static void ProcessPacket(object sender, List<PlaneData> e)
        {
            //вывод данных e.tableAeroscope
            Console.WriteLine("Data updated!");
            e.ForEach(p => Console.WriteLine("Icao: " + p.Icao + " AirId: " + p.AirId + " Lat: " + p.Lat + " Long: " + p.Long + " Height: " + p.Altitude + " Velocity: " + p.AirVelocity + " AirCat: " + p.AirCat + " Track angle: " + p.TrackAngle + " Ground speed:" + p.GroundSpeed));            
            Console.WriteLine("---------------------------------------------------------------------------------------------------------");

        }
    }
}

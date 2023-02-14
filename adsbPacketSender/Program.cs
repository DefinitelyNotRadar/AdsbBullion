using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace adsbPacketSender
{
    class Program
    {
        static async Task Main(string[] args)
        {

            IPAddress serverAddr = IPAddress.Parse("127.0.0.1");
            TcpListener server = new TcpListener(serverAddr, 30005);
            try
            {
                server.Start();
                Console.WriteLine("Сервер запущен. Ожидание подключений... ");
                while (true)
                {
                    // получаем подключение в виде TcpClient
                    TcpClient tcpClient = await server.AcceptTcpClientAsync();
                    Console.WriteLine($"Входящее подключение: {tcpClient.Client.RemoteEndPoint}");
                    // получаем объект NetworkStream для взаимодействия с клиентом
                    var stream = tcpClient.GetStream();
                    while (!Console.KeyAvailable)
                    {
                        //send DF17 different type packets

                        //send plane ID
                        byte[] bytestosendID = new byte[] { 0x2a, 0x8d, 0x48, 0x40, 0xd6, 0x20, 0x2c, 0xc3, 0x71, 0xc3, 0x28, 0xe0, 0x57, 0x60, 0x98, 0x3b };
                        await stream.WriteAsync(bytestosendID, 0, bytestosendID.Length);
                        Thread.Sleep(2000);

                        //send even air coordinates message
                        byte[] bytestosendEven = new byte[] { 0x2a, 0x8d, 0x40, 0x62, 0x1d, 0x58, 0xc3, 0x82, 0xd6, 0x90, 0xc8, 0xac, 0x28, 0x63, 0xa7, 0x3b };
                        await stream.WriteAsync(bytestosendEven, 0, bytestosendEven.Length);
                        
                        Thread.Sleep(2000);
                        
                        //send odd air coordinates message
                        byte[] bytestosendOdd = new byte[] { 0x2a, 0x8d, 0x40, 0x62, 0x1d, 0x58, 0xc3, 0x86, 0x43, 0x5c, 0xc4, 0x12, 0x69, 0x2a, 0xd6, 0x3b };
                        await stream.WriteAsync(bytestosendOdd, 0, bytestosendOdd.Length);

                        Thread.Sleep(2000);

                        //send GNSS plane height
                        byte[] bytestosendGnssHeight = new byte[] { 0x2a, 0x8d, 0x40, 0x62, 0x1d, 0x58, 0xc3, 0x82, 0xd6, 0x90, 0xC8, 0xac, 0x28, 0x63, 0xa7, 0x3b };
                        await stream.WriteAsync(bytestosendGnssHeight, 0, bytestosendGnssHeight.Length);


                        Console.WriteLine($"Клиенту {tcpClient.Client.RemoteEndPoint} отправлены данные");
                    }
                }
            }
            catch (ArgumentNullException e)
            {
                Console.WriteLine("ArgumentNullException: {0}", e);
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
            finally
            {
                server.Stop(); // останавливаем сервер
            }

            Console.WriteLine("\n Press Enter to continue...");
            Console.Read();
            
        }
    }
}

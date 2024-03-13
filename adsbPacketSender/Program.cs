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
                        //byte[] bytestosendID = new byte[] { 0x2a, 0x8d, 0x48, 0x40, 0xd6, 0x20, 0x2c, 0xc3, 0x71, 0xc3, 0x28, 0xe0, 0x57, 0x60, 0x98, 0x3b };
                        //await stream.WriteAsync(bytestosendID, 0, bytestosendID.Length);
                        //Thread.Sleep(2000);

                        ////send even air coordinates message
                        //byte[] bytestosendEven = new byte[] { 0x2a, 0x8d, 0x40, 0x62, 0x1d, 0x58, 0xc3, 0x82, 0xd6, 0x90, 0xc8, 0xac, 0x28, 0x63, 0xa7, 0x3b };
                        //await stream.WriteAsync(bytestosendEven, 0, bytestosendEven.Length);

                        //Thread.Sleep(2000);

                        ////send odd air coordinates message
                        //byte[] bytestosendOdd = new byte[] { 0x2a, 0x8d, 0x40, 0x62, 0x1d, 0x58, 0xc3, 0x86, 0x43, 0x5c, 0xc4, 0x12, 0x69, 0x2a, 0xd6, 0x3b };
                        //await stream.WriteAsync(bytestosendOdd, 0, bytestosendOdd.Length);

                        //Thread.Sleep(2000);

                        ////send GNSS plane height
                        //byte[] bytestosendGnssHeight = new byte[] { 0x2a, 0x8d, 0x40, 0x62, 0x1d, 0x58, 0xc3, 0x82, 0xd6, 0x90, 0xC8, 0xac, 0x28, 0x63, 0xa7, 0x3b };
                        //await stream.WriteAsync(bytestosendGnssHeight, 0, bytestosendGnssHeight.Length);


                        //byte[] bytestosendDF17TypeCode29 = new byte[] { 0x2a, 0x38, 0x44, 0x31, 0x35, 0x32, 0x30, 0x31, 0x43, 0x45, 0x41, 0x32, 0x35, 0x33, 0x38, 0x45, 0x38, 0x30, 0x31, 0x33, 0x43, 0x30, 0x38, 0x32, 0x46, 0x41, 0x43, 0x42, 0x36, 0x3b};
                        //await stream.WriteAsync(bytestosendDF17TypeCode29, 0, bytestosendDF17TypeCode29.Length);

                        //Thread.Sleep(2000);

                        //byte[] bytestosendDF17TypeCode29 = new byte[] { 0x2a, 0x38, 0x44, 0x31, 0x35, 0x32, 0x30, 0x31, 0x43, 0x45, 0x41, 0x32, 0x35, 0x33, 0x38, 0x45, 0x38, 0x30, 0x31, 0x33, 0x43, 0x30, 0x38, 0x32, 0x46, 0x41, 0x43, 0x42, 0x36, 0x3b };
                        //await stream.WriteAsync(bytestosendDF17TypeCode29, 0, bytestosendDF17TypeCode29.Length);

                        //Thread.Sleep(2000);

                        //byte[] bytestosendDF17TypeCode19 = new byte[] { 0x2a, 0x38, 0x44, 0x35, 0x31, 0x30, 0x30, 0x46, 0x43, 0x39, 0x39, 0x31, 0x31, 0x30, 0x43, 0x31, 0x30, 0x37, 0x30, 0x38, 0x30, 0x38, 0x38, 0x43, 0x36, 0x42, 0x36, 0x45, 0x43, 0x3b };
                        //await stream.WriteAsync(bytestosendDF17TypeCode19, 0, bytestosendDF17TypeCode19.Length);

                        //Thread.Sleep(2000);

                        //byte[] bytestosendDF17TypeCode11 = new byte[] { 0x2a, 0x38, 0x44, 0x34, 0x42, 0x42, 0x38, 0x36, 0x33, 0x35, 0x38, 0x33, 0x46, 0x30, 0x37, 0x46, 0x30, 0x43, 0x30, 0x32, 0x42, 0x31, 0x44, 0x42, 0x39, 0x38, 0x42, 0x33, 0x36, 0x3b };
                        //await stream.WriteAsync(bytestosendDF17TypeCode11, 0, bytestosendDF17TypeCode11.Length);

                        //Thread.Sleep(2000);

                        byte[] bytestosendDF17TypeCode11Even = new byte[] { 0x2a, 0x38, 0x44, 0x34, 0x30, 0x36, 0x32, 0x31, 0x44, 0x35, 0x38, 0x43, 0x33, 0x38, 0x32, 0x44, 0x36, 0x39, 0x30, 0x43, 0x38, 0x41, 0x43, 0x32, 0x38, 0x36, 0x33, 0x41, 0x37, 0x3b };
                        await stream.WriteAsync(bytestosendDF17TypeCode11Even, 0, bytestosendDF17TypeCode11Even.Length);

                        Thread.Sleep(5000);

                        byte[] bytestosendDF17TypeCode11Odd = new byte[] { 0x2a, 0x38, 0x44, 0x34, 0x30, 0x36, 0x32, 0x31, 0x44, 0x35, 0x38, 0x43, 0x33, 0x38, 0x36, 0x34, 0x33, 0x35, 0x43, 0x43, 0x34, 0x31, 0x32, 0x36, 0x39, 0x32, 0x41, 0x44, 0x36, 0x3b };
                        await stream.WriteAsync(bytestosendDF17TypeCode11Odd, 0, bytestosendDF17TypeCode11Odd.Length);

                        Thread.Sleep(6000);

                        //byte[] bytestosendDF17TypeCode = new byte[] { 0x2a, 0x38, 0x44, 0x34, 0x30, 0x36, 0x42, 0x39, 0x30, 0x32, 0x30, 0x31, 0x35, 0x41, 0x36, 0x37, 0x38, 0x44, 0x34, 0x44, 0x32, 0x32, 0x30, 0x41, 0x41, 0x34, 0x42, 0x44, 0x41, 0x3b };
                        //await stream.WriteAsync(bytestosendDF17TypeCode, 0, bytestosendDF17TypeCode.Length);


                        ///test DF20 and DF21 bds codes 50 and 60
                        //byte[] bytestosendDF20BdsCode50 = new byte[] { };
                        //await stream.WriteAsync(bytestosendDF20BdsCode50, 0, bytestosendDF20BdsCode50.Length);

                        //Thread.Sleep(2000);

                        //byte[] bytestosendDF20BdsCode60 = new byte[] { 0x2a, 0x41, 0x30, 0x30, 0x30, 0x31, 0x38, 0x33, 0x38, 0x45, 0x35, 0x31, 0x39, 0x46, 0x33, 0x33, 0x31, 0x36, 0x30, 0x32, 0x34, 0x30, 0x31, 0x34, 0x32, 0x44, 0x37, 0x46, 0x41, 0x3b };
                        //await stream.WriteAsync(bytestosendDF20BdsCode60, 0, bytestosendDF20BdsCode60.Length);

                        //byte[] bytestosendDF20BdsCode60 = new byte[] { 0x2a, 0x41, 0x38, 0x30, 0x30, 0x30, 0x34, 0x41, 0x41, 0x41, 0x37, 0x34, 0x41, 0x30, 0x37, 0x32, 0x42, 0x46, 0x44, 0x45, 0x46, 0x43, 0x31, 0x44, 0x35, 0x43, 0x42, 0x34, 0x46, 0x3b };
                        //await stream.WriteAsync(bytestosendDF20BdsCode60, 0, bytestosendDF20BdsCode60.Length);

                        //Thread.Sleep(4000);

                        //byte[] bytestosendDF21BdsCode50 = new byte[] { 0x2a, 0x41, 0x38, 0x30, 0x30, 0x31, 0x45, 0x42, 0x43, 0x46, 0x46, 0x46, 0x42, 0x32, 0x33, 0x32, 0x38, 0x36, 0x30, 0x30, 0x34, 0x41, 0x37, 0x33, 0x46, 0x36, 0x41, 0x35, 0x42, 0x3b };
                        //await stream.WriteAsync(bytestosendDF21BdsCode50, 0, bytestosendDF21BdsCode50.Length);

                        //Thread.Sleep(2000);

                        //byte[] bytestosendDF21BdsCode60 = new byte[] {  };
                        //await stream.WriteAsync(bytestosendDF21BdsCode60, 0, bytestosendDF21BdsCode60.Length);

                        //Thread.Sleep(4000);


                        //проверить более тщательно координаты, crc в df 17 и 18



                        Console.WriteLine($"Клиенту {tcpClient.Client.RemoteEndPoint} отправлены данные");
                        Thread.Sleep(4000);
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

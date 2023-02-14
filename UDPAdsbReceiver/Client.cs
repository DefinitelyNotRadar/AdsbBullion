using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YamlDotNet;
using Newtonsoft.Json;
using PlaneInfoLib;

namespace UDPAdsbReceiver
{
    public class Client
    {
        #region Events
        public event EventHandler<List<PlaneData>> OnUDPPacketReceived;//pocket received event
        public event EventHandler<string> OnOpenPort; // open port event
        public event EventHandler<string> OnClosePort; // close port event
        #endregion
        #region Variables
        public int LocalPort { get; set; } = 16000;
        public UdpClient udpClient;
        private Thread thrRead;
        private IPEndPoint remoteIp;
        public bool IsPortOpen = false;
        Yaml yaml = new Yaml();
        LocalProperties localProperties;
        #endregion
        public Client()
        {
            localProperties = yaml.YamlLoad();
        }
        public Client(int localPort)
        {
            try
            {
                localProperties = yaml.YamlLoad();
                localProperties.UdpPortLocal = localPort;
            }
            catch (Exception ex)
            {

            }
        }

        /// <summary>
        /// Start to receive UDP AeroScope data 
        /// </summary>
        #region Methods
        public void Listen()
        {
            try
            {
                if (udpClient != null)
                {
                    udpClient.Close();
                    udpClient = null;
                }
                udpClient = new UdpClient(localProperties.UdpPortLocal);
                IsPortOpen = true;
                OnOpenPort?.Invoke(this, "");
                if (thrRead != null)
                {
                    thrRead.Abort();
                    thrRead = null;
                }
                thrRead = new Thread(new ThreadStart(Receiving));
                thrRead.IsBackground = true;
                thrRead.Start();
            }
            catch (Exception ex)
            {
                Disconnect();
            }

        }

        /// <summary>
        /// Receiving UDP byte[] packets in created thread
        /// </summary>
        private async void Receiving()
        {
            while (true)
            {
                try
                {
                    byte[] data = null;
                    remoteIp = null;
                    data = udpClient.Receive(ref remoteIp);
                    Deserialize(data);
                }
                catch (Exception ex)
                {
                    Disconnect();
                }
            }
        }
        /// <summary>
        /// Deserialization of byte[] UDP packets
        /// </summary>
        /// <param name="newdata"></param>
        private void Deserialize(byte[] newdata)
        {
            try
            {
                string json = Encoding.UTF8.GetString(newdata);
                var data = JsonConvert.DeserializeObject<List<PlaneData>>(json);
                OnUDPPacketReceived?.Invoke(this, data);
            }
            catch (Exception ex)
            {

            }
        }
        /// <summary>
        /// Stop packets receiving
        /// </summary>
        public void Disconnect()
        {
            try
            {
                if (IsPortOpen == false)
                    return;
                if (thrRead != null)
                {
                    thrRead.Abort();
                    thrRead = null;
                }
                if (udpClient != null)
                {
                    udpClient.Close();
                    udpClient = null;
                }
                IsPortOpen = false;
                OnClosePort?.Invoke(this, "");
            }
            catch (Exception ex)
            {

            }
        }
        #endregion
    }
}

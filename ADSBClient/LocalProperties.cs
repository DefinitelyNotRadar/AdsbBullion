using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace ADSBClientLib
{
    public class LocalProperties
    {
        public string IPtcpMicroAdsb { get; set; }
        public int TcpPortMicroAdsb { get; set; }

        public string IPudpLocal { get; set; }
        public string IPudpRemote { get; set; }

        public int UdpPortLocal { get; set; }
        public int UdpPortRemote { get; set; }

        public string endPoint { get; set; }
        public string NameApplication { get; set; }


        public string WritingSerialNumber { get; set; }
        public bool IsWritingQuery { get; set; }

        public bool IsWriteDataToExcel { get; set; }

        /// <summary>
        /// for getting aeroscope packets on lib user side by udp
        /// </summary>
        public bool IsSend2UDPAdsbReceiver { get; set; }
        public int UDPAdsbReceiverPort { get; set; }
        public string UDPAdsbReceiverIp { get; set; }

        public string Hemisphere { get; set; }//North or South hemisphere, depends on where the receiver is physically located

        public double RefLatitude { get; set; }//minsk-2 airport latitude coordinate as reference latitude for adsb parser

        public double RefLongitude { get; set; }// minsk-2 airport longitude coordinate as reference longitude for adsb parser
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UDPAdsbReceiver
{
    public class PlaneData:EventArgs
    {
        public string Icao { get; set; } = "";
        public string AirId { get; set; } = "";
        public string AirCat { get; set; } = "";
        public int IsEven { get; set; }//-1(не задано), 0, 1
        public double LatEven { get; set; } = -1;
        public double LatOdd { get; set; } = -1;
        public double LongEven { get; set; } = -1;
        public double LongOdd { get; set; } = -1;
        public DateTime PackageReceiveTime { get; set; }
        public double TimeSpan { get; set; } = -1;
        public double TimeSpanEven { get; set; } = -1;
        public double TimeSpanOdd { get; set; } = -1;
        public double CPRLatEven { get; set; } = -1;
        public double CPRLongEven { get; set; } = -1;
        public double CPRLatOdd { get; set; } = -1;
        public double CPRLongOdd { get; set; } = -1;

        public double Lat { get; set; } = -1;
        public double Long { get; set; } = -1;
        public double Altitude { get; set; } = -1;
        public double GnssBaroHeightDelta { get; set; } = -1;//difference between baro and gnss heights in feets
        public double AirVelocity { get; set; } = -1;
        public double VerticalSpeed { get; set; } = -1;//in feets per minute
        public string AirStatus { get; set; } = "";
        public string TargetState { get; set; } = "";
        public string OperationStatus { get; set; } = "";
        public string DF { get; set; } = "";

        public double GrounfSpeed { get; set; } = -1;//in nautical miles(knotes)
        public double GrounfTrack { get; set; } = -1;//in degrees

        public double referenceLongitude { get; set; } = 27;//our adsb receiver longitude
        public double referenceLatitude { get; set; } = 54;//our adsb receiver latitude


        public PlaneData()
        {

        }
        public PlaneData(string icao, DateTime packageReceiveTime)
        {
            Icao = icao;
            PackageReceiveTime = packageReceiveTime;
        }

        public PlaneData(string icao)
        {
            Icao = icao;
        }

        public void UpdatePlaneInfo(int isEven, double latEven, double latOdd, double longEven, double longOdd, DateTime packageReceiveTime, double timeSpanEven, double timeSpanOdd, double cPRLat, double cPRLong, double lat, double lng, double height, double airVelocity, string airStatus, string targetState, string operationStatus, string dF)
        {

        }
    }
}

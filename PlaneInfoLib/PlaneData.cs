using System;

namespace PlaneInfoLib
{
    public class PlaneData
    {
        public string Icao { get; set; } = "";/// <summary>////!!!!!
        /// /!!!!!!!
        /// </summary>
        public string AirId { get; set; } = "";
        public string AirCat { get; set; } = "";
        public int IsEven { get; set; }//-1(не задано), 0, 1
        public double LatEven { get; set; } = -1;
        public double LatOdd { get; set; } = -1;
        public double LongEven { get; set; } = -1;
        public double LongOdd { get; set; } = -1;
        public DateTime PackageReceiveTime { get; set; } = DateTime.Now;
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
        public double VerticalRateSpeed { get; set; } = -1;//in feets per minute
        public int VerticalSpeedSign { get; set; } = -1;
        public int AirSpeedType { get; set; } = -1;
        public string AirStatus { get; set; } = "";
        public string TargetState { get; set; } = "";
        public string OperationStatus { get; set; } = "";
        public int TrackAngle { get; set; } = -1;/// <summary>////!!!!!!
        /// 
        /// </summary>
        public string DF { get; set; } = "";
        public int BarometricPressureSetting { get; set; } = -1;//is expressed in units of 0.1 inHg or 1 hPa
        public int IsOnGround { get; set; } = -1;
        public double GroundSpeed { get; set; } = -1;//in nautical miles(knotes)
        public double GroundTrack { get; set; } = -1;//in degrees

        public double ReferenceLongitude { get; set; } = 27;//our adsb receiver longitude
        public double ReferenceLatitude { get; set; } = 54;//our adsb receiver latitude

        public string TargetStateAndStatusMsg { get; set; } = "";//df17, typeCode 29

        public string Country { get; set; } = "";

        public string TransponderCapability { get; set; } = "";

        public int SquawkCode { get; set; } = -1;//varies from 0000 to 7777

        public PlaneData()
        {

        }
        public PlaneData(string icao, DateTime packageReceiveTime)
        {
            Icao = icao;
            PackageReceiveTime = packageReceiveTime;
        }

        public void UpdatePlaneInfo(int isEven, double latEven, double latOdd, double longEven, double longOdd, DateTime packageReceiveTime, double timeSpanEven, double timeSpanOdd, double cPRLat, double cPRLong, double lat, double lng, double height, double airVelocity, string airStatus, string targetState, string operationStatus, string dF)
        {

        }
    }
}

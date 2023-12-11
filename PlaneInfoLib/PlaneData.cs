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

        public bool IsLastGlobal { get; set; } = false;//shows wether the last coordinates were calculated like global coordinates 
        public double LatEven { get; set; } = -1;
        public double LatOdd { get; set; } = -1;
        public double LongEven { get; set; } = -1;
        public double LongOdd { get; set; } = -1;

        public double AcceptableDistance { get; set; } = -1;//in meteres, the distance which is calculated depending on the time passed since the last known position

        public string Hemisphere { get; set; } = "";
        public DateTime PackageReceiveTime { get; set; } = DateTime.Now;

        public DateTime PackageReceiveTimeEven { get; set; } = DateTime.MinValue;

        public DateTime PackageReceiveTimeOdd { get; set; } = DateTime.MinValue;

        public DateTime LastCoordTime { get; set; } = DateTime.MinValue;

        public double TimeSpan { get; set; } = -1;

        public double TimeSpanCoord { get; set; } = -1;//time since last coordinate was gotten
        public double TimeSpanEven { get; set; } = -1;
        public double TimeSpanOdd { get; set; } = -1;
        public double CPRLatEven { get; set; } = -1;
        public double CPRLongEven { get; set; } = -1;
        public double CPRLatOdd { get; set; } = -1;
        public double CPRLongOdd { get; set; } = -1;

        public double Lat { get; set; } = -1;
        public double Long { get; set; } = -1;
        public double LatOld { get; set; } = -1;
        public double LongOld { get; set; } = -1;
        public double Altitude { get; set; } = -1;
        public double GnssBaroHeightDelta { get; set; } = -1;//difference between baro and gnss heights in feets
        public double VerticalRateSpeed { get; set; } = -1;//in feets per minute
        public int VerticalSpeedSign { get; set; } = -1;
        public int AirSpeedType { get; set; } = -1;
        public string AirStatus { get; set; } = "";
        public string TargetState { get; set; } = "";
        public string OperationStatus { get; set; } = "";
        public double TrackAngle { get; set; } = -1;///in degrees, the direction of the planes route and i suppose it should be used on the map to draw the plane in according direction
                                                    /// 
                                                    /// </summary>
        public string DF { get; set; } = "";
        public int BarometricPressureSetting { get; set; } = -1;//is expressed in units of 0.1 inHg or 1 hPa
        public int IsOnGround { get; set; } = -1;
        public double GroundSpeed { get; set; } = -1;//in nautical miles(knotes)
        public double GroundTrack { get; set; } = -1;//in degrees

        public double ReferenceLongitude { get; set; } = -1;//our adsb receiver longitude
        public double ReferenceLatitude { get; set; } = -1;//our adsb receiver latitude

        public string TargetStateAndStatusMsg { get; set; } = "";//df17, typeCode 29

        public string Country { get; set; } = "";

        public string TransponderCapability { get; set; } = "";

        public int SquawkCode { get; set; } = -1;//varies from 0000 to 7777

        public bool IsHeadingAvailable { get; set; } = false;

        public string AirSpeedTypeName { get; set; } = "";

        public double MagneticHeading { get; set; } = -1;

        public double Heading { get; set; } = -1;//calculates using west and east speed components, in degrees?

        public double AirSpeed { get; set; } = -1;//in knots

        public double IC { get; set; } = -1; //Intent change flag - some field from airborne velocity message, not used?

        public double IFR { get; set; } = -1; //IFR capability flag - some field from airborne velocity message, not used?

        public double NUСr { get; set; } = -1; //NUCr is navigation uncertainty category for velocity, not used?

        public double Dew { get; set; } = -1; //Direction for E-W velocity component (0: from West to East, 1: from East to West)

        public double WestToEastSpeed { get; set; } = -1;//in knots
        public double EastToWestSpeed { get; set; } = -1;//in knots

        public double SouthToNorthSpeed { get; set; } = -1;//in knots
        public double NorthToSouthSpeed { get; set; } = -1;//in knots

        public double Dns { get; set; } = -1; //Direction for N-S velocity component Dns (0: from South to North, 1: from North to South)

        public double SH { get; set; } = -1;//status bit for magnetic heading for subrypes 3 and 4 of DF17 TC19 Airborne velocity packages

        public string FlightStatus { get; set; } = "";


        public string DownlinkRequest { get; set; } = "";

        public string Flag { get; set; } = "";//flag icon name like Argentina.png

        public string ModelName { get; set; } = "";//planes producer company name and planes model name

        public PlaneData()
        {

        }
        public PlaneData(string icao, DateTime packageReceiveTime)
        {
            Icao = icao;
            PackageReceiveTime = packageReceiveTime;
        }



        //public void UpdatePlaneInfo(int isEven, double latEven, double latOdd, double longEven, double longOdd, DateTime packageReceiveTime, double timeSpanEven, double timeSpanOdd, double cPRLat, double cPRLong, double lat, double lng, double height, double airVelocity, string airStatus, string targetState, string operationStatus, string dF)
        //{

        //}
    }
}


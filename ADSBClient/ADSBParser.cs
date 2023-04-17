using PlaneInfoLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ADSBClientLib
{
    public class ADSBParser
    {
        public ObservableCollection<PlaneData> listPlanes;

        public PlaneData planeData;
        DateTime receiveTime;
        
        public ADSBParser()
        {
            listPlanes = new ObservableCollection<PlaneData>();
            listPlanes.CollectionChanged += ListPlanes_CollectionChanged;
        }

        void ListPlanes_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            //list changed - an item was added.
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                
                //the new items are available in e.NewItems
            }
            if(e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Replace)
            {

            }
        }

        public bool ParseData(string hexstr)
        {
            try
            {
                receiveTime = DateTime.Now;

                byte[] hexstr2byte = Enumerable.Range(0, hexstr.Length)
                         .Where(x => x % 2 == 0)
                         .Select(x => Convert.ToByte(hexstr.Substring(x, 2), 16))
                         .ToArray();
                string asciiHexStr = Encoding.ASCII.GetString(hexstr2byte);               

                
                planeData = new PlaneData();

                double downlinkFormat = 0;
                downlinkFormat = (double)ulong.Parse(String.Concat(asciiHexStr[0], asciiHexStr[1]), System.Globalization.NumberStyles.HexNumber);
                downlinkFormat = (int)downlinkFormat >> 3;

                if (downlinkFormat == 17 || downlinkFormat == 11)//what are other df with transponderCapability data add them with || 
                {
                    double transponderCapability = 0;
                    transponderCapability = (double)ulong.Parse(String.Concat(asciiHexStr[0], asciiHexStr[1]), System.Globalization.NumberStyles.HexNumber);
                    transponderCapability = (int)transponderCapability & 7;
                    switch (transponderCapability)
                    {
                        case 0:
                            planeData.TransponderCapability = "Lvl 1 transponder";
                            break;
                        case 1 - 3:
                            planeData.TransponderCapability = "Reserved";
                            break;
                        case 4:
                            planeData.TransponderCapability = "Lvl2+ transponder. With ability to set CA to 7. On-ground";
                            break;
                        case 5:
                            planeData.TransponderCapability = "Lvl2+ transponder. With ability to set CA to 7. Airborne";
                            break;
                        case 6:
                            planeData.TransponderCapability = "Lvl2+ transponder. With ability to set CA to 7. On-ground or airborne";
                            break;
                        case 7:
                            planeData.TransponderCapability = "Downlink request value is 0 or Flight status is 2,3,4 or 5. On-ground or airborne";
                            break;
                        default: break;
                    }
                }
                     

                double typeCode = 0;
                typeCode = (double)ulong.Parse(String.Concat(asciiHexStr[8], asciiHexStr[9]), System.Globalization.NumberStyles.HexNumber);
                typeCode = (int)typeCode >> 3;

                if (downlinkFormat == 17)
                {

                    planeData.Icao = ParseICAO(asciiHexStr);
                    planeData = AddPlane();
                    if (typeCode >= 1 && typeCode <= 4)
                    {
                        try
                        {
                            ParseAirId(asciiHexStr.Substring(8, 14));
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                    if (typeCode >= 5 && typeCode <= 8)
                    {

                        planeData.PackageReceiveTime = receiveTime;
                        ParseSurfPos(asciiHexStr.Substring(8, 14));
                    }
                    if (typeCode >= 9 && typeCode <= 18)
                    {

                        planeData.PackageReceiveTime = receiveTime;
                        ParseAirPosBaro(asciiHexStr.Substring(8, 14));
                    }
                    if (typeCode >= 20 && typeCode <= 22)
                    {

                        planeData.PackageReceiveTime = receiveTime;
                        ParseAirPosGNSS(asciiHexStr.Substring(8, 14));
                    }
                    if (typeCode == 19)
                    {
                        ParseAirVelocity(asciiHexStr.Substring(8, 14));
                    }
                    if (typeCode == 28)
                    {
                        ParseAirStatus(asciiHexStr.Substring(8, 14));
                    }
                    if (typeCode == 29)
                    {
                        //ParseTargetStateAndStatus(hexstr.Substring(8, 14));
                    }
                    //if (typeCode == 31)
                    //{
                    //    ParseOperationStatus(hexstr.Substring(8, 21));
                    //}
                }

                if (downlinkFormat == 6)
                {

                    // Extract the various fields from the message
                    //int subtype = (message[1] & 0xF0) >> 4;
                    planeData.Icao = ParseICAO(asciiHexStr);

                    planeData = AddPlane();

                    //planeData.VerticalSpeedSign = ((message[4] & 0x08) == 0x08) ? -1 : 1;
                    //planeData.VerticalSpeed = (((message[4] & 0x07) << 6) | ((message[5] & 0xFC) >> 2)) * planeData.VerticalSpeedSign;
                    //planeData.AirSpeedType = message[5] & 0x03;
                    //planeData.AirVelocity = ((message[6] & 0x7F) << 3) | ((message[7] & 0xE0) >> 5);
                    //planeData.TrackAngle = ((message[7] & 0x1F) << 6) | ((message[8] & 0xFC) >> 2);
                    //planeData.GroundSpeed = ((message[8] & 0x03) << 8) | message[9];
                    //planeData.IsOnGround = (message[10] & 0x80) >> 7;
                    //planeData.Altitude = ((message[10] & 0x7F) << 4) | ((message[11] & 0xF0) >> 4);
                    //planeData.BarometricPressureSetting = ((message[11] & 0x0F) << 8) | message[12];
                    planeData.VerticalSpeedSign = ((asciiHexStr[7] & 0x08) == 0x08) ? -1 : 1;
                    planeData.VerticalRateSpeed = (((asciiHexStr[7] & 0x07) << 6) | ((asciiHexStr[8] & 0xFC) >> 2)) * planeData.VerticalSpeedSign;
                    planeData.AirSpeedType = asciiHexStr[8] & 0x03;
                    planeData.AirVelocity = ((asciiHexStr[9] & 0x7F) << 3) | ((asciiHexStr[10] & 0xE0) >> 5);
                    planeData.TrackAngle = ((asciiHexStr[10] & 0x1F) << 6) | ((asciiHexStr[11] & 0xFC) >> 2);
                    planeData.GroundSpeed = ((asciiHexStr[11] & 0x03) << 8) | asciiHexStr[12];
                    planeData.IsOnGround = (asciiHexStr[13] & 0x80) >> 7;
                    planeData.Altitude = ((asciiHexStr[13] & 0x7F) << 4) | ((asciiHexStr[14] & 0xF0) >> 4);
                    planeData.BarometricPressureSetting = ((asciiHexStr[14] & 0x0F) << 8) | asciiHexStr[15];

                }

                if (downlinkFormat == 4)
                {
                    //byte[] message = Enumerable.Range(0, asciiHexStr.Length)
                    //       .Where(x => x % 2 == 0)
                    //       .Select(x => Convert.ToByte(hexstr.Substring(x, 2), 16))
                    //       .ToArray();

                    //// Extract the 24-bit FLID value from the message
                    //uint flid = ((uint)message[1] << 16) | ((uint)message[2] << 8) | message[3];

                    //// Convert the FLID value to an ICAO address (in hexadecimal format)
                    //planeData.Icao = flid.ToString("X6");
                    //AddPlane();
                    //// Extract the position information from the message
                    //planeData.Lat = (int)(((uint)message[4] & 0xFE) << 13 | ((uint)message[5] & 0xFE) << 5 | ((uint)message[6] & 0xF8) >> 3);
                    //planeData.Long = (int)(((uint)message[6] & 0x07) << 18 | (uint)message[7] << 10 | (uint)message[8] << 2 | ((uint)message[9] & 0xC0) >> 6);
                    //planeData.Altitude = (int)(((uint)message[9] & 0x3F) << 4 | ((uint)message[10] & 0xF0) >> 4);

                    // Extract the NIC supplement, NACp, NACv, and NACp Static fields from the message
                    //int nicSupplement = (message[10] & 0x0F);
                    //int nacP = ((message[11] & 0xF0) >> 4);
                    //int nacV = (message[11] & 0x0F);
                    //int nacPStatic = ((message[12] & 0xF0) >> 4);



                }
                if (downlinkFormat == 5)
                {
                    string strSkuawk = asciiHexStr.Substring(4, 4);
                    int intSkuawk = int.Parse(strSkuawk, System.Globalization.NumberStyles.HexNumber);
                    intSkuawk = intSkuawk & 8191;

                    int A4 = (intSkuawk & 128)<<2;
                    int A2 = (intSkuawk & 512)<<1;
                    int A1 = intSkuawk & 2048;
                    int number4 = A4 | A2 | A1;

                    int B4 = (intSkuawk & 2) << 2;
                    int B2 = (intSkuawk & 8) << 1;
                    int B1 = intSkuawk & 64;
                    int number3 = B4 | B2 | B1;

                    int C4 = (intSkuawk & 256) << 2;
                    int C2 = (intSkuawk & 1024) << 1;
                    int C1 = intSkuawk & 4096;
                    int number2 = C4 | C2 | C1;

                    int D4 = (intSkuawk & 1) << 2;
                    int D2 = (intSkuawk & 4) << 1;
                    int D1 = intSkuawk & 16;
                    int number1 = D4 | D2 | D1;

                    int resultNumber = number4*1000 + number3*100 + number2*10 + number1;

                    planeData.SquawkCode = resultNumber;
                    planeData = AddPlane();

                }
                if (downlinkFormat == 11)
                {
                    planeData.Icao = ParseICAO(asciiHexStr);
                    planeData = AddPlane();
                }
            }
            catch (Exception ex)
            {
                return false;
            }
            return true;
        }


        private string ParseICAO(string bytestr)
        {
            string icao = "";
            return icao = String.Concat(bytestr[2], bytestr[3], bytestr[4], bytestr[5], bytestr[6], bytestr[7]);
        }

        private PlaneData AddPlane()
        {
            try
            {
                PlaneData match = listPlanes.Where(x => x.Icao.Contains(planeData.Icao)).FirstOrDefault();
                if (match == null)
                {
                    planeData.PackageReceiveTime = receiveTime;
                    listPlanes.Add(planeData);
                    return listPlanes.Last();
                }
                else
                {
                    planeData.TimeSpan = receiveTime.Subtract(match.PackageReceiveTime).TotalSeconds;
                    int index = listPlanes.IndexOf(listPlanes.First(a => a.Icao.Contains(planeData.Icao)));
                    listPlanes[index].TimeSpan = planeData.TimeSpan;                   
                    return listPlanes[index];
                }
            }
            catch(Exception ex)
            {
                return null;
            }
        }

        public int ParseDF(string str, int i)
        {
            double dF = (double)ulong.Parse(String.Concat(str[0], str[1]), System.Globalization.NumberStyles.HexNumber);
            dF = (int)dF >> 3;
            return (int)dF;
        }

        private void ParseAirId(string hexstr)
        {
            string airIdLetters = "#ABCDEFGHIJKLMNOPQRSTUVWXYZ##### ###############0123456789######";
            string strTCCA = hexstr.Substring(0, 2);
            int intTCCA = int.Parse(strTCCA, System.Globalization.NumberStyles.HexNumber);
            int intTC = intTCCA >> 3;
            int intCA = intTCCA & 7;//7 = 111 в двоичной, то есть (& 111) маска(логическое умножение) даст последние 3 бита от исходного числа или CA

            //air id and category intTC from 1 to 4
            if (intTC == 1)
            {
                planeData.AirCat = "Reserved";
            }

            if (intCA == 0)
            {
                planeData.AirCat = "No category information";
            }

            if (intTC == 2)
            {
                if (intCA == 1)
                {
                    planeData.AirCat = "Surface emergency vehicle";
                }
                if (intCA == 3)
                {
                    planeData.AirCat = "Surface service vehicle";
                }
                if (intCA >= 4 || intCA <= 7)
                {
                    planeData.AirCat = "Ground obstruction";
                }
            }

            if (intTC == 3)
            {
                if (intCA == 1)
                {
                    planeData.AirCat = "Glider, sailplane";
                }
                if (intCA == 2)
                {
                    planeData.AirCat = "Lighter-than-air";
                }
                if (intCA == 3)
                {
                    planeData.AirCat = "Parachutist, skydiver";
                }
                if (intCA == 4)
                {
                    planeData.AirCat = "Ultralight, hang-glider, paraglider";
                }
                if (intCA == 5)
                {
                    planeData.AirCat = "Reserved";
                }
                if (intCA == 6)
                {
                    planeData.AirCat = "Unmanned aerial vehicle";
                }
                if (intCA == 7)
                {
                    planeData.AirCat = "Space or transatmospheric vehicle";
                }
            }

            if (intTC == 4)
            {
                if (intCA == 1)
                {
                    planeData.AirCat = "Light (less than 7000 kg)/ICAO WTC L";
                }
                if (intCA == 2)
                {
                    planeData.AirCat = "Medium 1 (between 7000 kg and 34000 kg)/ICAO WTC M";
                }
                if (intCA == 3)
                {
                    planeData.AirCat = "Medium 2 (between 34000 kg to 136000 kg)/ICAO WTC M";
                }
                if (intCA == 4)
                {
                    planeData.AirCat = "High vortex aircraft/ICAO WTC H (Heavy) or J (Super)";
                }
                if (intCA == 5)
                {
                    planeData.AirCat = "Heavy (larger than 136000 kg)/ICAO WTC H (Heavy) or J (Super)";
                }
                if (intCA == 6)
                {
                    planeData.AirCat = "High performance (>5 g acceleration) and high speed (>400 kt)";
                }
                if (intCA == 7)
                {
                    planeData.AirCat = "Rotorcraft";
                }
            }

            for (int i = 2; i < hexstr.Length; i = i + 3)
            {
                string strLetter1 = hexstr.Substring(i, 1);
                int intLetter1 = int.Parse(strLetter1, System.Globalization.NumberStyles.HexNumber);
                intLetter1 = intLetter1 >> 2;
                planeData.AirId = planeData.AirId + airIdLetters[intLetter1];

                if (i + 1 < hexstr.Length)
                {
                    int intLetter2 = (int.Parse(strLetter1, System.Globalization.NumberStyles.HexNumber) << 2) | (int.Parse(hexstr.Substring(i + 1, 1), System.Globalization.NumberStyles.HexNumber) >> 2);
                    planeData.AirId = planeData.AirId + airIdLetters[intLetter2];
                }


                if (i + 2 < hexstr.Length)
                {
                    int intLetter3 = ((int.Parse(hexstr.Substring(i + 1, 1), System.Globalization.NumberStyles.HexNumber) & 3) << 4) | int.Parse(hexstr.Substring(i + 2, 1), System.Globalization.NumberStyles.HexNumber);
                    planeData.AirId = planeData.AirId + airIdLetters[intLetter3];
                }

            }
            planeData.AirId = planeData.AirId.Replace("#", "");
        }

        //Ground position
        private void ParseSurfPos(string hexstr)
        {

            //Movement
            string strMov = hexstr.Substring(1, 2);
            int intMov = int.Parse(strMov, System.Globalization.NumberStyles.HexNumber);
            intMov = intMov & 127;

            if (intMov == 0)
            {
                planeData.GroundSpeed = -1;
            }
            if (intMov == 1)
            {
                planeData.GroundSpeed = 0;
            }
            if (intMov >= 2 && intMov <= 8)
            {
                planeData.GroundSpeed = 0.125 + (intMov - 2) * 0.125;
            }
            if (intMov >= 9 && intMov <= 12)
            {
                planeData.GroundSpeed = 1 + (intMov - 9) * 0.25;
            }
            if (intMov >= 13 && intMov <= 38)
            {
                planeData.GroundSpeed = 2 + (intMov - 13) * 0.5;
            }
            if (intMov >= 39 && intMov <= 93)
            {
                planeData.GroundSpeed = 15 + (intMov - 39) * 1;
            }
            if (intMov >= 94 && intMov <= 108)
            {
                planeData.GroundSpeed = 70 + (intMov - 94) * 2;
            }
            if (intMov >= 109 && intMov <= 123)
            {
                planeData.GroundSpeed = 100 + (intMov - 109) * 5;
            }
            if (intMov == 124)
            {
                planeData.GroundSpeed = 175;//speed is 175 nautical miles(knotes) or more
            }
            if (intMov >= 125 && intMov <= 127)
            {
                planeData.GroundSpeed = -1;//??reserved:)
            }

            //Track
            string strTrkStatus = hexstr.Substring(3, 1);
            int intTrkStatus = int.Parse(strTrkStatus, System.Globalization.NumberStyles.HexNumber);
            intTrkStatus = intTrkStatus >> 3;

            if (intTrkStatus == 1)
            {
                string strTrk = hexstr.Substring(3, 2);
                int intTrk = int.Parse(strTrk, System.Globalization.NumberStyles.HexNumber);
                intTrk = intTrk & 127;

                planeData.GroundTrack = 360 * intTrk / 180;//in degrees
            }

            //Ground Coordinates
            string strF = hexstr.Substring(5, 1);
            int intF = int.Parse(strF, System.Globalization.NumberStyles.HexNumber);
            intF = (intF >> 2) & 1;


            //latitude and longitude globally(when previouse coordinate is unknown or further than 10sec) and locally(when previouse coordinate is known and calculated less then 10 sec before)
            //even latitude & long locally
            if (planeData.TimeSpan >= 0 && planeData.TimeSpan <= 15 && planeData.CPRLatEven != -1 && planeData.CPRLongEven != -1 && planeData.Lat != -1 && planeData.Long != -1 && intF == 0)
            {
                double dLat = 90 / (4 * 15);
                double j = Math.Floor(planeData.Lat / dLat) + Math.Floor(Mod(planeData.Lat, dLat) / dLat - planeData.CPRLatEven + 0.5);
                planeData.Lat = dLat * (j + planeData.CPRLatEven);

                double dLon = 90 / Math.Max(NL(planeData.Lat), 1);
                double m = Math.Floor(planeData.Long / dLon) + Math.Floor(Mod(planeData.Long, dLon) / dLon - planeData.CPRLongEven + 0.5);
                planeData.Long = dLon * (m + planeData.CPRLongEven);
            }
            //even global position message
            else
            {
                if (intF == 0)
                {
                    if (planeData.CPRLatOdd != -1)
                    {
                        string strCPRLat = hexstr.Substring(5, 9);
                        long intCPRLat = long.Parse(strCPRLat, System.Globalization.NumberStyles.HexNumber);
                        intCPRLat = (intCPRLat >> 1) & 131071;
                        planeData.CPRLatEven = (double)intCPRLat / 131072;

                        string strCPRLong = hexstr.Substring(9, 5);
                        long intCPRLong = long.Parse(strCPRLong, System.Globalization.NumberStyles.HexNumber);
                        intCPRLong = intCPRLong & 131071;
                        planeData.CPRLongEven = (double)intCPRLong / 131072;

                        double j = Math.Floor(59 * planeData.CPRLatEven - 60 * planeData.CPRLatOdd + 0.5);
                        planeData.LatEven = 90 * (Mod(j, 60) + planeData.CPRLatEven) / (4 * 15);


                        if (NL(planeData.LatEven) == NL(planeData.LatOdd))//the odd and even lat coord are in the same lat/long? zone otherwise we cant calculate lat
                        {
                            planeData.Lat = planeData.LatEven;



                            //calculate global longitude
                            double m = Math.Floor(planeData.CPRLongEven * (NL(planeData.Lat) - 1) - planeData.CPRLongOdd * NL(planeData.Lat) + 0.5);
                            double nEven = Math.Max(NL(planeData.Lat), 1);
                            double dLonEven = 90 / nEven;
                            planeData.LongEven = dLonEven * (Mod(m, nEven) + planeData.CPRLongEven);

                            List<double> list = new List<double> { planeData.LongEven, planeData.LongEven + 90, planeData.LongEven + 180, planeData.LongEven + 270 };   //возможные долготы самолета                          
                            planeData.Long = list.Aggregate((x, y) => Math.Abs(x - planeData.ReferenceLongitude) < Math.Abs(y - planeData.ReferenceLongitude) ? x : y);//ближайшая долгота самолета из возможных к долготе приемника адсб будет верной
                        }
                    }
                    else
                    {
                        string strCPRLat = hexstr.Substring(5, 9);
                        long intCPRLat = long.Parse(strCPRLat, System.Globalization.NumberStyles.HexNumber);
                        intCPRLat = (intCPRLat >> 1) & 131071;
                        planeData.CPRLatEven = (double)intCPRLat / 131072;

                        string strCPRLong = hexstr.Substring(9, 13);
                        long intCPRLong = long.Parse(strCPRLong, System.Globalization.NumberStyles.HexNumber);
                        intCPRLong = intCPRLong & 131071;
                        planeData.CPRLongEven = (double)intCPRLong / 131072;
                    }
                }
            }
            //odd latitude & long locally
            if (planeData.TimeSpan >= 0 && planeData.TimeSpan <= 15 && planeData.CPRLatOdd != -1 && planeData.CPRLongOdd != -1 && planeData.Long != -1 && planeData.Lat != -1 && intF == 1)
            {
                double dLat = 90 / (4 * 15 - 1);
                double j = Math.Floor(planeData.Lat / dLat) + Math.Floor(Mod(planeData.Lat, dLat) / dLat - planeData.CPRLatOdd + 0.5);
                planeData.Lat = dLat * (j + planeData.CPRLatOdd);

                double dLon = 90 / Math.Max(NL(planeData.Lat) - 1, 1);
                double m = Math.Floor(planeData.Long / dLon) + Math.Floor(Mod(planeData.Long, dLon) / dLon - planeData.CPRLongOdd + 0.5);
                planeData.Long = dLon * (m + planeData.CPRLongOdd);
            }
            //odd global position message
            else
            {
                if (intF == 1)
                {
                    if (planeData.CPRLatEven != -1)
                    {
                        string strCPRLat = hexstr.Substring(5, 9);
                        long intCPRLat = long.Parse(strCPRLat, System.Globalization.NumberStyles.HexNumber);
                        intCPRLat = (intCPRLat >> 1) & 131071;
                        planeData.CPRLatEven = (double)intCPRLat / 131072;

                        string strCPRLong = hexstr.Substring(9, 5);
                        long intCPRLong = long.Parse(strCPRLong, System.Globalization.NumberStyles.HexNumber);
                        intCPRLong = intCPRLong & 131071;
                        planeData.CPRLongEven = (double)intCPRLong / 131072;

                        double j = Math.Floor(59 * planeData.CPRLatEven - 60 * planeData.CPRLatOdd + 0.5);
                        planeData.LatOdd = 90 * (Mod(j, 59) + planeData.CPRLatOdd) / (4 * 15 - 1);

                        if (NL(planeData.LatEven) == NL(planeData.LatOdd))//the odd and even lat coord are in the same lat zone otherwise we cant calculate lat
                        {
                            planeData.Lat = planeData.LatOdd;



                            //calculate global longitude
                            double m = Math.Floor(planeData.CPRLongEven * (NL(planeData.Lat) - 1) - planeData.CPRLongOdd * NL(planeData.Lat) + 0.5);
                            double nOdd = Math.Max(NL(planeData.Lat) - 1, 1);
                            double dLonOdd = 90 / nOdd;
                            planeData.LongOdd = dLonOdd * (Mod(m, nOdd) + planeData.CPRLongOdd);

                            List<double> list = new List<double> { planeData.LongEven, planeData.LongEven + 90, planeData.LongEven + 180, planeData.LongEven + 270 };   //возможные долготы самолета                          
                            planeData.Long = list.Aggregate((x, y) => Math.Abs(x - planeData.ReferenceLongitude) < Math.Abs(y - planeData.ReferenceLongitude) ? x : y);//ближайшая долгота самолета из возможных к долготе приемника адсб будет верной

                        }
                    }
                    else
                    {
                        string strCPRLat = hexstr.Substring(5, 9);
                        long intCPRLat = long.Parse(strCPRLat, System.Globalization.NumberStyles.HexNumber);
                        intCPRLat = (intCPRLat >> 1) & 131071;
                        planeData.CPRLatEven = (double)intCPRLat / 131072;

                        string strCPRLong = hexstr.Substring(9, 13);
                        long intCPRLong = long.Parse(strCPRLong, System.Globalization.NumberStyles.HexNumber);
                        intCPRLong = intCPRLong & 131071;
                        planeData.CPRLongEven = (double)intCPRLong / 131072;
                    }
                }
            }



        }

        private void ParseTargetStateAndSatatus(string hexstr)
        {
            //    bool SILSupplement = false;
            //    string SelectedAltitudeText = "";

            //    string strTmp = hexstr.Substring(4, 2);
            //    int intTmp = int.Parse(strTmp, System.Globalization.NumberStyles.HexNumber);
            //    intTmp = (intTmp >> 1) & 3;

            //    if(intTmp!=1)
            //    {
            //        planeData.TargetStateAndStatusMsg = "Reserved"; 
            //    }
            //    else
            //    {
            //        strTmp = hexstr.Substring(4, 2);
            //        intTmp = int.Parse(strTmp, System.Globalization.NumberStyles.HexNumber);
            //        intTmp = (int)(intTmp & 1);
            //        if(intTmp==0)
            //        {
            //            SILSupplement = false;
            //        }
            //        else
            //        {
            //            SILSupplement = true;
            //        }

            //        strTmp = hexstr.Substring(6, 2);
            //        intTmp = int.Parse(strTmp, System.Globalization.NumberStyles.HexNumber);
            //        intTmp = (int)((intTmp>>7) & 1);
            //        if(intTmp==0)
            //        {
            //            SelectedAltitudeText = SelectedAltitudeText + "Altitude data provider: MCP/FCU" + System.Environment.NewLine;
            //        }
            //        else
            //        {
            //            SelectedAltitudeText = SelectedAltitudeText + "Altitude data provider: FMS" + System.Environment.NewLine;
            //        }

            //        strTmp = hexstr.Substring(8, 2);
            //        intTmp = int.Parse(strTmp, System.Globalization.NumberStyles.HexNumber);
            //        intTmp = (int)((intTmp >> 4) & 2047);

            //        if(intTmp==0)
            //        {
            //            SelectedAltitudeText = SelectedAltitudeText + "The altitude data doesn't exist or unavailable" +System.Environment.NewLine;
            //        }
            //        else
            //        {
            //            intTmp = (intTmp - 1) * 32;
            //            SelectedAltitudeText = SelectedAltitudeText + "Altitude : " + intTmp + "feet" + System.Environment.NewLine;
            //        }

            //        strTmp = hexstr.Substring(10, 2);
            //        intTmp = int.Parse(strTmp, System.Globalization.NumberStyles.HexNumber);
            //        intTmp = (int)((intTmp >> 3) & 2047);
            //    }
        }


        //Air position
        private void ParseAirPosBaro(string hexstr)
        {
            try
            {
                string strF = hexstr.Substring(5, 1);
                int intF = int.Parse(strF, System.Globalization.NumberStyles.HexNumber);
                intF = (intF >> 2) & 1;

                //coordinates                
                //latitude and longitude globally(when previouse coordinate is unknown or further than 10sec) and locally(when previouse coordinate is known and calculated less then 10 sec before)
                
                //even latitude & long locally
                if (planeData.TimeSpan >= 0 && planeData.TimeSpan <= 15 && planeData.CPRLatEven != -1 && planeData.CPRLongEven != -1 && intF == 0 && planeData.Lat != -1 && planeData.Long != -1)
                {
                    double dLat = 360 / (4 * 15);
                    double j = Math.Floor(planeData.Lat / dLat) + Math.Floor(Mod(planeData.Lat, dLat) / dLat - planeData.CPRLatEven + 0.5);
                    planeData.Lat = dLat * (j + planeData.CPRLatEven);

                    double dLon = 360 / Math.Max(NL(planeData.Lat), 1);
                    double m = Math.Floor(planeData.Long / dLon) + Math.Floor(Mod(planeData.Long, dLon) / dLon - planeData.CPRLongEven + 0.5);
                    planeData.Long = dLon * (m + planeData.CPRLongEven);
                }
                //even global position message
                else
                {
                    if (intF == 0)
                    {
                        if (planeData.CPRLatOdd != -1)
                        {
                            string strCPRLat = hexstr.Substring(5, 5);
                            long longCPRLat = long.Parse(strCPRLat, System.Globalization.NumberStyles.HexNumber);
                            longCPRLat = (longCPRLat >> 1) & 131071;
                            planeData.CPRLatEven = (double)longCPRLat / 131072;

                            string strCPRLong = hexstr.Substring(9, 5);
                            long longCPRLong = long.Parse(strCPRLong, System.Globalization.NumberStyles.HexNumber);
                            longCPRLong = longCPRLong & 131071;
                            planeData.CPRLongEven = (double)longCPRLong / 131072;

                            double j = Math.Floor(59 * planeData.CPRLatEven - 60 * planeData.CPRLatOdd + 0.5);
                            planeData.LatEven = 360 * (Mod(j, 60) + planeData.CPRLatEven) / (4 * 15);


                            if (planeData.LatEven >= 270)
                            {
                                planeData.LatEven = planeData.LatEven - 360;
                            }
                            if (NL(planeData.LatEven) == NL(planeData.LatOdd))//the odd and even lat coord are in the same lat/long? zone otherwise we cant calculate lat
                            {
                                planeData.Lat = planeData.LatEven;



                                //calculate global longitude
                                double a = NL(planeData.Lat);
                                double m = Math.Floor(planeData.CPRLongEven * (NL(planeData.Lat) - 1) - planeData.CPRLongOdd * NL(planeData.Lat) + 0.5);
                                double nEven = Math.Max(NL(planeData.Lat), 1);
                                double dLonEven = 360 / nEven;
                                planeData.LongEven = dLonEven * (Mod(m, nEven) + planeData.CPRLongEven);
                                if (planeData.LongEven >= 180)
                                {
                                    planeData.LongEven = planeData.LongEven - 360;
                                }
                                planeData.Long = planeData.LongEven;
                            }
                        }
                        else
                        {
                            string strCPRLat = hexstr.Substring(5, 5);
                            long longCPRLat = long.Parse(strCPRLat, System.Globalization.NumberStyles.HexNumber);
                            longCPRLat = (longCPRLat >> 1) & 131071;
                            planeData.CPRLatEven = (double)longCPRLat / 131072;

                            string strCPRLong = hexstr.Substring(9, 5);
                            long longCPRLong = long.Parse(strCPRLong, System.Globalization.NumberStyles.HexNumber);
                            longCPRLong = longCPRLong & 131071;
                            planeData.CPRLongEven = (double)longCPRLong / 131072;
                        }
                    }
                }
                //odd latitude & long locally
                if (planeData.TimeSpan >= 0 && planeData.TimeSpan <= 10 && planeData.CPRLatOdd != -1 && planeData.CPRLongOdd != -1 && intF == 1 && planeData.Lat!=-1 && planeData.Long!=-1)
                {
                    double dLat = 360 / (4 * 15 - 1);
                    double j = Math.Floor(planeData.Lat / dLat) + Math.Floor(Mod(planeData.Lat, dLat) / dLat - planeData.CPRLatOdd + 0.5);
                    planeData.Lat = dLat * (j + planeData.CPRLatOdd);

                    double dLon = 360 / Math.Max(NL(planeData.Lat) - 1, 1);
                    double m = Math.Floor(planeData.Long / dLon) + Math.Floor(Mod(planeData.Long, dLon) / dLon - planeData.CPRLongOdd + 0.5);
                    planeData.Long = dLon * (m + planeData.CPRLongOdd);
                }
                //odd global position message
                else
                {
                    if (intF == 1)
                    {
                        if (planeData.CPRLatEven != -1)
                        {
                            string strCPRLat = hexstr.Substring(5, 5);
                            long longCPRLat = long.Parse(strCPRLat, System.Globalization.NumberStyles.HexNumber);
                            longCPRLat = (longCPRLat >> 1) & 131071;
                            planeData.CPRLatOdd = (double)longCPRLat / 131072;

                            string strCPRLong = hexstr.Substring(9, 5);
                            long longCPRLong = long.Parse(strCPRLong, System.Globalization.NumberStyles.HexNumber);
                            longCPRLong = longCPRLong & 131071;
                            planeData.CPRLongOdd = (double)longCPRLong / 131072;

                            double j = Math.Floor(59 * planeData.CPRLatEven - 60 * planeData.CPRLatOdd + 0.5);
                            planeData.LatOdd = 360 * (Mod(j, 59) + planeData.CPRLatOdd) / (4 * 15 - 1);

                            if (planeData.LatOdd >= 270)
                            {
                                planeData.LatOdd = planeData.LatOdd - 360;
                            }
                            if (NL(planeData.LatEven) == NL(planeData.LatOdd))//the odd and even lat coord are in the same lat zone otherwise we cant calculate lat
                            {
                                planeData.Lat = planeData.LatOdd;



                                //calculate global longitude
                                double m = Math.Floor(planeData.CPRLongEven * (NL(planeData.Lat) - 1) - planeData.CPRLongOdd * NL(planeData.Lat) + 0.5);
                                double nOdd = Math.Max(NL(planeData.Lat) - 1, 1);
                                double dLonOdd = 360 / nOdd;
                                planeData.LongOdd = dLonOdd * (Mod(m, nOdd) + planeData.CPRLongOdd);
                                if (planeData.LongOdd >= 180)
                                {
                                    planeData.LongOdd = planeData.LongOdd - 360;
                                }
                                planeData.Long = planeData.LongOdd;
                            }
                        }
                        else
                        {
                            string strCPRLat = hexstr.Substring(5, 5);
                            long longCPRLat = long.Parse(strCPRLat, System.Globalization.NumberStyles.HexNumber);
                            longCPRLat = (longCPRLat >> 1) & 131071;
                            planeData.CPRLatOdd = (double)longCPRLat / 131072;

                            string strCPRLong = hexstr.Substring(9, 5);
                            long longCPRLong = long.Parse(strCPRLong, System.Globalization.NumberStyles.HexNumber);
                            longCPRLong = longCPRLong & 131071;
                            planeData.CPRLongOdd = (double)longCPRLong / 131072;
                        }
                    }
                }

                string strQ = hexstr.Substring(3, 1);
                int intQ = int.Parse(strQ, System.Globalization.NumberStyles.HexNumber);
                intQ = intQ & 1;

                string strAlt = hexstr.Substring(3, 3);
                int intAlt = int.Parse(strAlt, System.Globalization.NumberStyles.HexNumber);
                intAlt = ((intAlt & 4064) >> 1) | (intAlt & 15);

                if (intQ == 1)
                {
                    planeData.Altitude = (25 * intAlt - 1000) * 0.3048;//translate feets to meters *0.3048
                }

                if (intQ == 0)
                {
                    planeData.Altitude = (100 * intAlt - 1000) * 0.3048;
                }
            }
            catch (Exception ex)
            {
                planeData.Altitude = -1;
            }
        }
        private void ParseAirPosGNSS(string hexstr)
        {
            try
            {
                string strF = hexstr.Substring(5, 1);
                int intF = int.Parse(strF, System.Globalization.NumberStyles.HexNumber);
                intF = (intF >> 2) & 1;

                //coordinates
                //

                //latitude and longitude globally(when previouse coordinate is unknown or further than 10sec) and locally(when previouse coordinate is known and calculated less then 10 sec before)
                //even latitude & long locally
                if (planeData.TimeSpan >= 0 && planeData.TimeSpan <= 15 && planeData.CPRLatEven != -1 && planeData.CPRLongEven != -1 && planeData.Lat!=-1 && planeData.Long!=-1 && intF == 0)
                {
                    double dLat = 360 / (4 * 15);
                    double j = Math.Floor(planeData.Lat / dLat) + Math.Floor(Mod(planeData.Lat, dLat) / dLat - planeData.CPRLatEven + 0.5);
                    planeData.Lat = dLat * (j + planeData.CPRLatEven);

                    double dLon = 360 / Math.Max(NL(planeData.Lat), 1);
                    double m = Math.Floor(planeData.Long / dLon) + Math.Floor(Mod(planeData.Long, dLon) / dLon - planeData.CPRLongEven + 0.5);
                    planeData.Long = dLon * (m + planeData.CPRLongEven);
                }
                //even global position message
                else
                {
                    if (intF == 0)
                    {
                        if (planeData.CPRLatOdd != -1)
                        {
                            string strCPRLong = hexstr.Substring(5, 5);
                            long intCPRLong = long.Parse(strCPRLong, System.Globalization.NumberStyles.HexNumber);
                            intCPRLong = (intCPRLong >> 1) & 131071;
                            planeData.CPRLongEven = (double)intCPRLong / 131072;

                            string strCPRLat = hexstr.Substring(9, 5);
                            long intCPRLat = long.Parse(strCPRLat, System.Globalization.NumberStyles.HexNumber);
                            intCPRLat = intCPRLat & 131071;
                            planeData.CPRLatEven = (double)intCPRLat / 131072;

                            double j = Math.Floor(59 * planeData.CPRLatEven - 60 * planeData.CPRLatOdd + 0.5);
                            planeData.LatEven = 360 * (Mod(j, 60) + planeData.CPRLatEven) / (4 * 15);


                            if (planeData.LatEven >= 270)
                            {
                                planeData.LatEven = planeData.LatEven - 360;
                            }
                            if (NL(planeData.LatEven) == NL(planeData.LatOdd))//the odd and even lat coord are in the same lat/long? zone otherwise we cant calculate lat
                            {
                                planeData.Lat = planeData.LatEven;



                                //calculate global longitude
                                double m = Math.Floor(planeData.CPRLongEven * (NL(planeData.Lat) - 1) - planeData.CPRLongOdd * NL(planeData.Lat) + 0.5);
                                double nEven = Math.Max(NL(planeData.Lat), 1);
                                double dLonEven = 360 / nEven;
                                planeData.LongEven = dLonEven * (Mod(m, nEven) + planeData.CPRLongEven);
                                if (planeData.LongEven >= 180)
                                {
                                    planeData.LongEven = planeData.LongEven - 360;
                                }
                                planeData.Long = planeData.LongEven;
                            }
                        }
                        else
                        {
                            string strCPRLong = hexstr.Substring(5, 5);
                            long intCPRLong = long.Parse(strCPRLong, System.Globalization.NumberStyles.HexNumber);
                            intCPRLong = (intCPRLong >> 1) & 131071;
                            planeData.CPRLongEven = (double)intCPRLong / 131072;

                            string strCPRLat = hexstr.Substring(9, 5);
                            long intCPRLat = long.Parse(strCPRLat, System.Globalization.NumberStyles.HexNumber);
                            intCPRLat = intCPRLat & 131071;
                            planeData.CPRLatEven = (double)intCPRLat / 131072;
                        }
                    }
                }
                //odd latitude & long locally
                if (planeData.TimeSpan >= 0 && planeData.TimeSpan <= 15 && planeData.CPRLongOdd != -1 && planeData.CPRLatOdd != -1 && planeData.Long!=-1 && planeData.Lat!=-1 && intF == 1)
                {
                    double dLat = 360 / (4 * 15 - 1);
                    double j = Math.Floor(planeData.Lat / dLat) + Math.Floor(Mod(planeData.Lat, dLat) / dLat - planeData.CPRLatOdd + 0.5);
                    planeData.Lat = dLat * (j + planeData.CPRLatOdd);

                    double dLon = 360 / Math.Max(NL(planeData.Lat) - 1, 1);
                    double m = Math.Floor(planeData.Long / dLon) + Math.Floor(Mod(planeData.Long, dLon) / dLon - planeData.CPRLongEven + 0.5);
                    planeData.Long = dLon * (m + planeData.CPRLongEven);
                }
                //odd global position message
                else
                {
                    if (intF == 1)
                    {
                        if (planeData.CPRLatEven != -1)
                        {
                            string strCPRLong = hexstr.Substring(5, 5);
                            int intCPRLong = int.Parse(strCPRLong, System.Globalization.NumberStyles.HexNumber);
                            intCPRLong = (intCPRLong >> 1) & 131071;
                            planeData.CPRLongOdd = intCPRLong / 131072;

                            string strCPRLat = hexstr.Substring(9, 5);
                            int intCPRLat = int.Parse(strCPRLat, System.Globalization.NumberStyles.HexNumber);
                            intCPRLat = intCPRLat & 131071;
                            planeData.CPRLatOdd = intCPRLat / 131072;

                            double j = Math.Floor(59 * planeData.CPRLatEven - 60 * planeData.CPRLatOdd + 0.5);
                            planeData.LatOdd = 360 * (Mod(j, 59) + planeData.CPRLatOdd) / (4 * 15 - 1);

                            if (planeData.LatOdd >= 270)
                            {
                                planeData.LatOdd = planeData.LatOdd - 360;
                            }
                            if (NL(planeData.LatEven) == NL(planeData.LatOdd))//the odd and even lat coord are in the same lat zone otherwise we cant calculate lat
                            {
                                planeData.Lat = planeData.LatOdd;



                                //calculate global longitude
                                double m = Math.Floor(planeData.CPRLongEven * (NL(planeData.Lat) - 1) - planeData.CPRLongOdd * NL(planeData.Lat) + 0.5);
                                double nOdd = Math.Max(NL(planeData.Lat) - 1, 1);
                                double dLonOdd = 360 / nOdd;
                                planeData.LongOdd = dLonOdd * (Mod(m, nOdd) + planeData.CPRLongOdd);
                                if (planeData.LongOdd >= 180)
                                {
                                    planeData.LongOdd = planeData.LongOdd - 360;
                                }
                                planeData.Long = planeData.LongOdd;
                            }
                        }
                        else
                        {
                            string strCPRLong = hexstr.Substring(5, 5);
                            int intCPRLong = int.Parse(strCPRLong, System.Globalization.NumberStyles.HexNumber);
                            intCPRLong = (intCPRLong >> 1) & 131071;
                            planeData.CPRLongOdd = intCPRLong / 131072;

                            string strCPRLat = hexstr.Substring(9, 5);
                            int intCPRLat = int.Parse(strCPRLat, System.Globalization.NumberStyles.HexNumber);
                            intCPRLat = intCPRLat & 131071;
                            planeData.CPRLatOdd = intCPRLat / 131072;
                        }
                    }
                }

                string strAlt = hexstr.Substring(3, 3);
                int intAlt = int.Parse(strAlt, System.Globalization.NumberStyles.HexNumber);
                planeData.Altitude = intAlt;//in meters by default
            }
            catch (Exception ex)
            {

            }
        }

        private void ParseAirVelocity(string hexstr)
        {

            string strVr = hexstr.Substring(9, 3);
            int intVr = int.Parse(strVr, System.Globalization.NumberStyles.HexNumber);
            intVr = (intVr >> 2) & 511;

            string strSvr = hexstr.Substring(9, 1);
            int intSvr = int.Parse(strSvr, System.Globalization.NumberStyles.HexNumber);
            intSvr = intSvr >> 3;

            if (intSvr == 0)
            {
                planeData.VerticalRateSpeed = 64 * (intVr - 1);
            }
            if (intSvr == 1)
            {
                planeData.VerticalRateSpeed = -64 * (intVr - 1);
            }

            string strSign = hexstr.Substring(12, 1);
            int intSign = int.Parse(strSign, System.Globalization.NumberStyles.HexNumber);
            intSign = intSign >> 3;

            string strDelta = hexstr.Substring(12, 2);
            int intDelta = int.Parse(strDelta, System.Globalization.NumberStyles.HexNumber);
            intDelta = intDelta & 127;

            if (intSign == 1)
            {
                planeData.GnssBaroHeightDelta = -(intDelta - 1) * 25;
            }
            else if (intSign == 0 && intDelta != 0)
            {
                planeData.GnssBaroHeightDelta = (intDelta - 1) * 25;
            }
            else
            {
                planeData.GnssBaroHeightDelta = -1;//info isnt available
            }
            planeData = AddPlane();
        }
        private void ParseGroundVelocity(string hexstr)
        {

        }

        private string ParseAirStatus(string byteData)
        {
            return null;
        }
        private string ParseTargetState(string byteData)
        {
            return null;
        }
        private string ParseAirOperationStatus(string byteData)
        {
            return null;
        }

        private double Mod(double x, double y)
        {
            if (y != 0)
            {
                return x - y * Math.Floor(x / y);
            }
            else return 0;
        }
        private double NL(double lat)
        {
            if(lat == 0)
            {
                return 59;
            }
            if(lat == 87 || lat == -87)
            {
                return 2;
            }
            if(lat > 87)
            {
                return 1;
            }
            if (lat < -87)
            {
                return 1;
            }
            else
            {
                return Math.Floor(2 * Math.PI / Math.Acos(1 - (1 - Math.Cos(Math.PI / (2 * 15))) / Math.Pow(Math.Cos(Math.PI * lat / 180), 2)));
            }
                
        }
    }
}

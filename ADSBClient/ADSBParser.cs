using PlaneInfoLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

using HtmlAgilityPack;
using OfficeOpenXml;
using System.ComponentModel;
using LicenseContext = OfficeOpenXml.LicenseContext;
using System.IO.Packaging;
using System.Xml.XPath;
using System.Numerics;
using System.Windows;
using CsvHelper;
using TinyCsvParser;
using YamlDotNet.Core.Tokens;
using Microsoft.VisualBasic.FileIO;
using System.Runtime.Remoting.Channels;
using OfficeOpenXml.Drawing.Slicer.Style;
using System.Reflection;

///TODO: Add db with icao and country of registration to receive plane registration by ICAO
namespace ADSBClientLib
{
    public class ADSBParser
    {
        public ObservableCollection<PlaneData> listPlanes;

        public PlaneData planeData;
        DateTime receiveTime;
        DateTime prevPacketTime;
        LocalProperties localProperties;
        Yaml yaml = new Yaml();

        public ADSBParser()
        {
            listPlanes = new ObservableCollection<PlaneData>();
            listPlanes.CollectionChanged += ListPlanes_CollectionChanged;
            localProperties = yaml.YamlLoad();

            CalcPermutations1(88);
            CalcPermutations2(88);
            //CalcPermutations3(88);
            //CalcPermutations4(88);
            //CalcPermutations5(88);
        }

        void ListPlanes_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            ////list changed - an item was added.
            //if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            //{
                
            //    //the new items are available in e.NewItems
            //}
            //if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Replace)
            //{

            //}
        }

        double downlinkFormat = 0;
        public bool ParseData(string hexstr, DateTime msgTime)
        {
            try
            {
                receiveTime = msgTime;

                byte[] hexstr2byte = Enumerable.Range(0, hexstr.Length)
                         .Where(x => x % 2 == 0)
                         .Select(x => Convert.ToByte(hexstr.Substring(x, 2), 16))
                         .ToArray();
                string asciiDecodedHexStr = Encoding.ASCII.GetString(hexstr2byte);//перевод закодированного буллионом по таблице ascii сообщения в его первоначальный вид, в hex-е

                //Console.WriteLine(asciiDecodedHexStr);

                //проверка и исправление входящего пакета
                bool isBitError = !CheckCRC(asciiDecodedHexStr, 17);
                if (isBitError)
                {
                    if (!FixPacket(asciiDecodedHexStr, errorSyndrome.ToString()))
                    {
                        return false;
                    }
                }

                planeData = new PlaneData();
                planeData.ReferenceLatitude = localProperties.RefLatitude;
                planeData.ReferenceLongitude = localProperties.RefLongitude;
                planeData.Hemisphere = localProperties.Hemisphere;

               
                downlinkFormat = (double)ulong.Parse(String.Concat(asciiDecodedHexStr[0], asciiDecodedHexStr[1]), System.Globalization.NumberStyles.HexNumber);
                downlinkFormat = (int)downlinkFormat >> 3;

                //check whether the received df17/18 message is correct               
                //if ((downlinkFormat == 17 && !CheckCRC(asciiDecodedHexStr, 17)) || (downlinkFormat == 18 && !CheckCRC(asciiDecodedHexStr, 18)))
             

                //if (downlinkFormat == 17 || downlinkFormat == 18)// || downlinkFormat == 11)//what are other df with transponderCapability data add them with || 
                //{
                //    double transponderCapability = 0;
                //    transponderCapability = (double)ulong.Parse(String.Concat(asciiDecodedHexStr[0], asciiDecodedHexStr[1]), System.Globalization.NumberStyles.HexNumber);
                //    transponderCapability = (int)transponderCapability & 7;
                //    switch (transponderCapability)
                //    {
                //        case 0:
                //            planeData.TransponderCapability = "Lvl 1 transponder";
                //            break;
                //        case 1 - 3:
                //            planeData.TransponderCapability = "Reserved";
                //            break;
                //        case 4:
                //            planeData.TransponderCapability = "Lvl2+ transponder. With ability to set CA to 7. On-ground";
                //            break;
                //        case 5:
                //            planeData.TransponderCapability = "Lvl2+ transponder. With ability to set CA to 7. Airborne";
                //            break;
                //        case 6:
                //            planeData.TransponderCapability = "Lvl2+ transponder. With ability to set CA to 7. On-ground or airborne";
                //            break;
                //        case 7:
                //            planeData.TransponderCapability = "Downlink request value is 0 or Flight status is 2,3,4 or 5. On-ground or airborne";
                //            break;
                //        default: break;
                //    }
                //}

                double typeCode = 0;
                if (downlinkFormat == 17 || downlinkFormat == 18)
                {
                    typeCode = (double)ulong.Parse(String.Concat(asciiDecodedHexStr[8], asciiDecodedHexStr[9]), System.Globalization.NumberStyles.HexNumber);
                    typeCode = (int)typeCode >> 3;

                    //Console.WriteLine(typeCode);

                    planeData.Icao = ParseICAO(asciiDecodedHexStr);                    
                    if (planeData.Icao.Length > 0)
                    {
                        planeData = GetPlane()!=null?GetPlane():planeData;//take an old record of the plane with the same icao if exists
                        planeData.LatOld = planeData.Lat;
                        planeData.LongOld = planeData.Long;
                        prevPacketTime = planeData.PackageReceiveTime;
                        planeData.PackageReceiveTime = receiveTime;
                        
                    }
                    else
                    {
                        return false;
                    }

                    double transponderCapability = 0;
                    transponderCapability = (double)ulong.Parse(String.Concat(asciiDecodedHexStr[0], asciiDecodedHexStr[1]), System.Globalization.NumberStyles.HexNumber);
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

                    if (typeCode >= 1 && typeCode <= 4)
                    {
                        try
                        {
                            planeData.PackageReceiveTime = receiveTime;
                            ParseAirId(asciiDecodedHexStr.Substring(8, 14));
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                    if (typeCode >= 5 && typeCode <= 8)
                    {

                        planeData.PackageReceiveTime = receiveTime;
                        ParseSurfPos(asciiDecodedHexStr.Substring(8, 14));
                    }
                    if (typeCode >= 9 && typeCode <= 18)
                    {

                        planeData.PackageReceiveTime = receiveTime;
                        ParseAirPosBaro(asciiDecodedHexStr.Substring(8, 14));
                    }
                    if (typeCode >= 20 && typeCode <= 22)
                    {

                        planeData.PackageReceiveTime = receiveTime;
                        ParseAirPosGNSS(asciiDecodedHexStr.Substring(8, 14));
                    }
                    if (typeCode == 19)
                    {
                        planeData.PackageReceiveTime = receiveTime;
                        ParseAirVelocity(asciiDecodedHexStr.Substring(8, 14));
                    }
                    if (typeCode == 28)
                    {
                        planeData.PackageReceiveTime = receiveTime;
                        ParseAirStatus(asciiDecodedHexStr.Substring(8, 14));
                    }
                    if (typeCode == 29)
                    {
                        //ParseTargetStateAndStatus(hexstr.Substring(8, 14));
                    }
                    //if (typeCode == 31)
                    //{
                    //    ParseOperationStatus(hexstr.Substring(8, 21));
                    //}
                    AddPlane();
                }

                //if (downlinkFormat == 11)
                //{
                //    planeData.Icao = ParseICAO(asciiDecodedHexStr);
                //    planeData = AddPlane();

                //    string binarystring = String.Join(String.Empty, hexstr.Select(c => Convert.ToString(Convert.ToInt32(c.ToString(), 16), 2).PadLeft(4, '0')));//переводим 16 ичные данные пакета в их двоичный вид
                //    StringBuilder sbbinarystring = new StringBuilder(binarystring);


                //    //calculating crc of 112 bits received packet, should be zero if no errors have occured during transmission
                //    for (int i = 0; i <= sbbinarystring.Length - 1 - 24; i++)
                //    {

                //        if (sbbinarystring[i].Equals('1'))
                //        {
                //            for (int j = 0; j < polynom.Length; j++)
                //            {

                //                if (sbbinarystring[i + j].Equals(polynom[j])) { sbbinarystring[i + j] = '0'; }
                //                else sbbinarystring[i + j] = '1';

                //            }
                //        }
                //    }

                //    string strCrc = sbbinarystring.ToString();
                //    planeData.InterrogatorIdCode = (int)Convert.ToInt64(strCrc, 2);
                //}

                //if (downlinkFormat == 6)
                //{

                //    // Extract the various fields from the message
                //    //int subtype = (message[1] & 0xF0) >> 4;
                //    planeData.Icao = ParseICAO(asciiDecodedHexStr);

                //    planeData = AddPlane();

                //    //planeData.VerticalSpeedSign = ((message[4] & 0x08) == 0x08) ? -1 : 1;
                //    //planeData.VerticalSpeed = (((message[4] & 0x07) << 6) | ((message[5] & 0xFC) >> 2)) * planeData.VerticalSpeedSign;
                //    //planeData.AirSpeedType = message[5] & 0x03;
                //    //planeData.AirVelocity = ((message[6] & 0x7F) << 3) | ((message[7] & 0xE0) >> 5);
                //    //planeData.TrackAngle = ((message[7] & 0x1F) << 6) | ((message[8] & 0xFC) >> 2);
                //    //planeData.GroundSpeed = ((message[8] & 0x03) << 8) | message[9];
                //    //planeData.IsOnGround = (message[10] & 0x80) >> 7;
                //    //planeData.Altitude = ((message[10] & 0x7F) << 4) | ((message[11] & 0xF0) >> 4);
                //    //planeData.BarometricPressureSetting = ((message[11] & 0x0F) << 8) | message[12];
                //    planeData.VerticalSpeedSign = ((asciiDecodedHexStr[7] & 0x08) == 0x08) ? -1 : 1;
                //    planeData.VerticalRateSpeed = (((asciiDecodedHexStr[7] & 0x07) << 6) | ((asciiDecodedHexStr[8] & 0xFC) >> 2)) * planeData.VerticalSpeedSign;
                //    planeData.AirSpeedType = asciiDecodedHexStr[8] & 0x03;
                //    planeData.AirVelocity = ((asciiDecodedHexStr[9] & 0x7F) << 3) | ((asciiDecodedHexStr[10] & 0xE0) >> 5);
                //    planeData.TrackAngle = ((asciiDecodedHexStr[10] & 0x1F) << 6) | ((asciiDecodedHexStr[11] & 0xFC) >> 2);
                //    planeData.GroundSpeed = ((asciiDecodedHexStr[11] & 0x03) << 8) | asciiDecodedHexStr[12];
                //    planeData.IsOnGround = (asciiDecodedHexStr[13] & 0x80) >> 7;
                //    planeData.Altitude = ((asciiDecodedHexStr[13] & 0x7F) << 4) | ((asciiDecodedHexStr[14] & 0xF0) >> 4);
                //    planeData.BarometricPressureSetting = ((asciiDecodedHexStr[14] & 0x0F) << 8) | asciiDecodedHexStr[15];

                //}

                //if (downlinkFormat == 4)
                //{
                //    int flightStatus = (int)ulong.Parse(hexstr.Substring(2, 1), System.Globalization.NumberStyles.HexNumber);
                //    flightStatus = flightStatus & 7;

                //    switch (flightStatus)
                //    {
                //        case 0:
                //            planeData.FlightStatus = "no alert, no SPI, aircraft is airborne";
                //            break;
                //        case 1:
                //            planeData.FlightStatus = "no alert, no SPI, aircraft is on-ground";
                //            break;
                //        case 2:
                //            planeData.FlightStatus = "alert, no SPI, aircraft is airborne";
                //            break;
                //        case 3:
                //            planeData.FlightStatus = "alert, no SPI, aircraft is on-ground";
                //            break;
                //        case 4:
                //            planeData.FlightStatus = "alert, SPI, aircraft is airborne or on-ground";
                //            break;
                //        case 5:
                //            planeData.FlightStatus = "no alert, SPI, aircraft is airborne or on-ground";
                //            break;
                //        case 6:
                //            planeData.FlightStatus = "reserved";
                //            break;
                //        case 7:
                //            planeData.FlightStatus = "not assigned";
                //            break;
                //    }

                //    int downlinkRequest = (int)ulong.Parse(hexstr.Substring(3, 1), System.Globalization.NumberStyles.HexNumber);
                //    downlinkRequest = downlinkRequest >> 3;

                //    switch (downlinkRequest)
                //    {
                //        case 0:
                //            planeData.DownlinkRequest = "no downlink request";
                //            break;
                //        case 1:
                //            planeData.DownlinkRequest = "request to send Comm-B message";
                //            break;
                //        case 4:
                //            planeData.DownlinkRequest = "Comm-B broadcast message 1 available";
                //            break;
                //        case 5:
                //            planeData.DownlinkRequest = "Comm-B broadcast message 2 available";
                //            break;
                //    }

                //    int utilityMessage = (int)ulong.Parse(hexstr.Substring(3, 2), System.Globalization.NumberStyles.HexNumber);
                //    utilityMessage = (utilityMessage >> 5) & 63;

                //    switch (utilityMessage)
                //    {
                //        case 0:
                //            planeData.DownlinkRequest = "no downlink request";
                //            break;
                //        case 1:
                //            planeData.DownlinkRequest = "request to send Comm-B message";
                //            break;
                //        case 4:
                //            planeData.DownlinkRequest = "Comm-B broadcast message 1 available";
                //            break;
                //        case 5:
                //            planeData.DownlinkRequest = "Comm-B broadcast message 2 available";
                //            break;
                //    }


                //    planeData = AddPlane();




                //}
                //if (downlinkFormat == 5)
                //{
                //    int flightStatus = (int)ulong.Parse(hexstr.Substring(2, 1), System.Globalization.NumberStyles.HexNumber);
                //    flightStatus = flightStatus & 7;

                //    switch (flightStatus)
                //    {
                //        case 0:
                //            planeData.FlightStatus = "no alert, no SPI, aircraft is airborne";
                //            break;
                //        case 1:
                //            planeData.FlightStatus = "no alert, no SPI, aircraft is on-ground";
                //            break;
                //        case 2:
                //            planeData.FlightStatus = "alert, no SPI, aircraft is airborne";
                //            break;
                //        case 3:
                //            planeData.FlightStatus = "alert, no SPI, aircraft is on-ground";
                //            break;
                //        case 4:
                //            planeData.FlightStatus = "alert, SPI, aircraft is airborne or on-ground";
                //            break;
                //        case 5:
                //            planeData.FlightStatus = "no alert, SPI, aircraft is airborne or on-ground";
                //            break;
                //        case 6:
                //            planeData.FlightStatus = "reserved";
                //            break;
                //        case 7:
                //            planeData.FlightStatus = "not assigned";
                //            break;
                //    }

                //    int downlinkRequest = (int)ulong.Parse(hexstr.Substring(3, 1), System.Globalization.NumberStyles.HexNumber);
                //    downlinkRequest = downlinkRequest >> 3;

                //    switch (downlinkRequest)
                //    {
                //        case 0:
                //            planeData.DownlinkRequest = "no downlink request";
                //            break;
                //        case 1:
                //            planeData.DownlinkRequest = "request to send Comm-B message";
                //            break;
                //        case 4:
                //            planeData.DownlinkRequest = "Comm-B broadcast message 1 available";
                //            break;
                //        case 5:
                //            planeData.DownlinkRequest = "Comm-B broadcast message 2 available";
                //            break;
                //    }

                //    int utilityMessage = (int)ulong.Parse(hexstr.Substring(3, 2), System.Globalization.NumberStyles.HexNumber);
                //    utilityMessage = (utilityMessage >> 5) & 63;

                //    switch (utilityMessage)
                //    {
                //        case 0:
                //            planeData.DownlinkRequest = "no downlink request";
                //            break;
                //        case 1:
                //            planeData.DownlinkRequest = "request to send Comm-B message";
                //            break;
                //        case 4:
                //            planeData.DownlinkRequest = "Comm-B broadcast message 1 available";
                //            break;
                //        case 5:
                //            planeData.DownlinkRequest = "Comm-B broadcast message 2 available";
                //            break;
                //    }
                //    planeData = AddPlane();

                //}
                if (downlinkFormat == 20)
                {

                    bool isCorrectMsg = CheckCRC(hexstr, 20);
                    if(isCorrectMsg==false)
                    {
                        return false;
                    }

                    planeData = GetPlane()!=null?GetPlane():planeData;
                    planeData.LatOld = planeData.Lat;
                    planeData.LongOld = planeData.Long;

                    int flightStatus = (int)ulong.Parse(hexstr.Substring(2, 1), System.Globalization.NumberStyles.HexNumber);
                    flightStatus = flightStatus & 7;

                    switch (flightStatus)
                    {
                        case 0:
                            planeData.FlightStatus = "no alert, no SPI, aircraft is airborne";
                            break;
                        case 1:
                            planeData.FlightStatus = "no alert, no SPI, aircraft is on-ground";
                            break;
                        case 2:
                            planeData.FlightStatus = "alert, no SPI, aircraft is airborne";
                            break;
                        case 3:
                            planeData.FlightStatus = "alert, no SPI, aircraft is on-ground";
                            break;
                        case 4:
                            planeData.FlightStatus = "alert, SPI, aircraft is airborne or on-ground";
                            break;
                        case 5:
                            planeData.FlightStatus = "no alert, SPI, aircraft is airborne or on-ground";
                            break;
                        case 6:
                            planeData.FlightStatus = "reserved";
                            break;
                        case 7:
                            planeData.FlightStatus = "not assigned";
                            break;
                    }

                    int downlinkRequest = (int)ulong.Parse(hexstr.Substring(3, 1), System.Globalization.NumberStyles.HexNumber);
                    downlinkRequest = downlinkRequest >> 3;

                    switch (downlinkRequest)
                    {
                        case 0:
                            planeData.DownlinkRequest = "no downlink request";
                            break;
                        case 1:
                            planeData.DownlinkRequest = "request to send Comm-B message";
                            break;
                        case 4:
                            planeData.DownlinkRequest = "Comm-B broadcast message 1 available";
                            break;
                        case 5:
                            planeData.DownlinkRequest = "Comm-B broadcast message 2 available";
                            break;
                    }

                    int utilityMessage = (int)ulong.Parse(hexstr.Substring(3, 2), System.Globalization.NumberStyles.HexNumber);
                    utilityMessage = (utilityMessage >> 5) & 63;

                    switch (utilityMessage)
                    {
                        case 0:
                            planeData.DownlinkRequest = "no downlink request";
                            break;
                        case 1:
                            planeData.DownlinkRequest = "request to send Comm-B message";
                            break;
                        case 4:
                            planeData.DownlinkRequest = "Comm-B broadcast message 1 available";
                            break;
                        case 5:
                            planeData.DownlinkRequest = "Comm-B broadcast message 2 available";
                            break;
                    }

                    AddPlane();
                }
                if (downlinkFormat == 21)
                {
                    bool isCorrectMsg = CheckCRC(hexstr, 21);
                    if (isCorrectMsg == false)
                    {
                        return false;
                    }
                    int flightStatus = (int)ulong.Parse(hexstr.Substring(2, 1), System.Globalization.NumberStyles.HexNumber);
                    flightStatus = flightStatus & 7;

                    switch (flightStatus)
                    {
                        case 0:
                            planeData.FlightStatus = "no alert, no SPI, aircraft is airborne";
                            break;
                        case 1:
                            planeData.FlightStatus = "no alert, no SPI, aircraft is on-ground";
                            break;
                        case 2:
                            planeData.FlightStatus = "alert, no SPI, aircraft is airborne";
                            break;
                        case 3:
                            planeData.FlightStatus = "alert, no SPI, aircraft is on-ground";
                            break;
                        case 4:
                            planeData.FlightStatus = "alert, SPI, aircraft is airborne or on-ground";
                            break;
                        case 5:
                            planeData.FlightStatus = "no alert, SPI, aircraft is airborne or on-ground";
                            break;
                        case 6:
                            planeData.FlightStatus = "reserved";
                            break;
                        case 7:
                            planeData.FlightStatus = "not assigned";
                            break;
                    }

                    int downlinkRequest = (int)ulong.Parse(hexstr.Substring(3, 1), System.Globalization.NumberStyles.HexNumber);
                    downlinkRequest = downlinkRequest >> 3;

                    switch (downlinkRequest)
                    {
                        case 0:
                            planeData.DownlinkRequest = "no downlink request";
                            break;
                        case 1:
                            planeData.DownlinkRequest = "request to send Comm-B message";
                            break;
                        case 4:
                            planeData.DownlinkRequest = "Comm-B broadcast message 1 available";
                            break;
                        case 5:
                            planeData.DownlinkRequest = "Comm-B broadcast message 2 available";
                            break;
                    }

                    int utilityMessage = (int)ulong.Parse(hexstr.Substring(3, 2), System.Globalization.NumberStyles.HexNumber);
                    utilityMessage = (utilityMessage >> 5) & 63;

                    switch (utilityMessage)
                    {
                        case 0:
                            planeData.DownlinkRequest = "no downlink request";
                            break;
                        case 1:
                            planeData.DownlinkRequest = "request to send Comm-B message";
                            break;
                        case 4:
                            planeData.DownlinkRequest = "Comm-B broadcast message 1 available";
                            break;
                        case 5:
                            planeData.DownlinkRequest = "Comm-B broadcast message 2 available";
                            break;
                    }

                    planeData = AddPlane();
                }
                if (planeData.Icao.Length >= 6) { return true; }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
          
        }

        /// <summary>
        /// parse Dima adsb data(data are ready to parse)
        /// </summary>
        /// <param name="bytestr"></param>
        /// <returns></returns>
        public bool ParseDimaData(string asciiHexStr, DateTime msgTime)
        {
            try
            {
                receiveTime = msgTime;

                
                //Console.WriteLine(asciiHexStr);

                planeData = new PlaneData();
                planeData.ReferenceLatitude = localProperties.RefLatitude;
                planeData.ReferenceLongitude = localProperties.RefLongitude;
                planeData.Hemisphere = localProperties.Hemisphere;

                double downlinkFormat = 0;
                downlinkFormat = (double)ulong.Parse(String.Concat(asciiHexStr[0], asciiHexStr[1]), System.Globalization.NumberStyles.HexNumber);
                downlinkFormat = (int)downlinkFormat >> 3;

                //check whether the received df17 message is correct
                //if ((downlinkFormat == 17 && !CheckCRC(asciiDecodedHexStr, 17)) || (downlinkFormat == 18 && !CheckCRC(asciiDecodedHexStr, 18)))
                //{
                //    return false;
                //}

                //if (downlinkFormat == 17 || downlinkFormat == 18)// || downlinkFormat == 11)//what are other df with transponderCapability data add them with || 
                //{
                //    double transponderCapability = 0;
                //    transponderCapability = (double)ulong.Parse(String.Concat(asciiDecodedHexStr[0], asciiDecodedHexStr[1]), System.Globalization.NumberStyles.HexNumber);
                //    transponderCapability = (int)transponderCapability & 7;
                //    switch (transponderCapability)
                //    {
                //        case 0:
                //            planeData.TransponderCapability = "Lvl 1 transponder";
                //            break;
                //        case 1 - 3:
                //            planeData.TransponderCapability = "Reserved";
                //            break;
                //        case 4:
                //            planeData.TransponderCapability = "Lvl2+ transponder. With ability to set CA to 7. On-ground";
                //            break;
                //        case 5:
                //            planeData.TransponderCapability = "Lvl2+ transponder. With ability to set CA to 7. Airborne";
                //            break;
                //        case 6:
                //            planeData.TransponderCapability = "Lvl2+ transponder. With ability to set CA to 7. On-ground or airborne";
                //            break;
                //        case 7:
                //            planeData.TransponderCapability = "Downlink request value is 0 or Flight status is 2,3,4 or 5. On-ground or airborne";
                //            break;
                //        default: break;
                //    }
                //}

                double typeCode = 0;
                if (downlinkFormat == 17 || downlinkFormat == 18)
                {
                    typeCode = (double)ulong.Parse(String.Concat(asciiHexStr[8], asciiHexStr[9]), System.Globalization.NumberStyles.HexNumber);
                    typeCode = (int)typeCode >> 3;

                    //Console.WriteLine(typeCode);

                    planeData.Icao = ParseICAO(asciiHexStr);
                    if (planeData.Icao.Length > 0)
                    {
                        planeData = GetPlane() != null ? GetPlane() : planeData;//take an old record of the plane with the same icao if exists
                        planeData.LatOld = planeData.Lat;
                        planeData.LongOld = planeData.Long;
                        prevPacketTime = planeData.PackageReceiveTime;
                        planeData.PackageReceiveTime = receiveTime;

                    }
                    else
                    {
                        return false;
                    }

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

                    if (typeCode >= 1 && typeCode <= 4)
                    {
                        try
                        {
                            planeData.PackageReceiveTime = receiveTime;
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
                        planeData.PackageReceiveTime = receiveTime;
                        ParseAirVelocity(asciiHexStr.Substring(8, 14));
                    }
                    if (typeCode == 28)
                    {
                        planeData.PackageReceiveTime = receiveTime;
                        ParseAirStatus(asciiHexStr.Substring(8, 14));
                    }
                    if (typeCode == 29)
                    {
                        //ParseTargetStateAndStatus(hexstr.Substring(8, 14));
                    }
                 
                    AddPlane();
                }

                string hexstr = ConvertAsciiToHex(asciiHexStr);
                if (downlinkFormat == 20)
                {
                   

                    //bool isCorrectMsg = CheckCRC(hexstr, 20);
                    //if (isCorrectMsg == false)
                    //{
                    //    return false;
                    //}

                    planeData = GetPlane() != null ? GetPlane() : planeData;
                    planeData.LatOld = planeData.Lat;
                    planeData.LongOld = planeData.Long;

                    int flightStatus = (int)ulong.Parse(hexstr.Substring(2, 1), System.Globalization.NumberStyles.HexNumber);
                    flightStatus = flightStatus & 7;

                    switch (flightStatus)
                    {
                        case 0:
                            planeData.FlightStatus = "no alert, no SPI, aircraft is airborne";
                            break;
                        case 1:
                            planeData.FlightStatus = "no alert, no SPI, aircraft is on-ground";
                            break;
                        case 2:
                            planeData.FlightStatus = "alert, no SPI, aircraft is airborne";
                            break;
                        case 3:
                            planeData.FlightStatus = "alert, no SPI, aircraft is on-ground";
                            break;
                        case 4:
                            planeData.FlightStatus = "alert, SPI, aircraft is airborne or on-ground";
                            break;
                        case 5:
                            planeData.FlightStatus = "no alert, SPI, aircraft is airborne or on-ground";
                            break;
                        case 6:
                            planeData.FlightStatus = "reserved";
                            break;
                        case 7:
                            planeData.FlightStatus = "not assigned";
                            break;
                    }

                    int downlinkRequest = (int)ulong.Parse(hexstr.Substring(3, 1), System.Globalization.NumberStyles.HexNumber);
                    downlinkRequest = downlinkRequest >> 3;

                    switch (downlinkRequest)
                    {
                        case 0:
                            planeData.DownlinkRequest = "no downlink request";
                            break;
                        case 1:
                            planeData.DownlinkRequest = "request to send Comm-B message";
                            break;
                        case 4:
                            planeData.DownlinkRequest = "Comm-B broadcast message 1 available";
                            break;
                        case 5:
                            planeData.DownlinkRequest = "Comm-B broadcast message 2 available";
                            break;
                    }

                    int utilityMessage = (int)ulong.Parse(hexstr.Substring(3, 2), System.Globalization.NumberStyles.HexNumber);
                    utilityMessage = (utilityMessage >> 5) & 63;

                    switch (utilityMessage)
                    {
                        case 0:
                            planeData.DownlinkRequest = "no downlink request";
                            break;
                        case 1:
                            planeData.DownlinkRequest = "request to send Comm-B message";
                            break;
                        case 4:
                            planeData.DownlinkRequest = "Comm-B broadcast message 1 available";
                            break;
                        case 5:
                            planeData.DownlinkRequest = "Comm-B broadcast message 2 available";
                            break;
                    }

                    AddPlane();
                }
                if (downlinkFormat == 21)
                {
                    //bool isCorrectMsg = CheckCRC(hexstr, 21);
                    //if (isCorrectMsg == false)
                    //{
                    //    return false;
                    //}
                    int flightStatus = (int)ulong.Parse(hexstr.Substring(2, 1), System.Globalization.NumberStyles.HexNumber);
                    flightStatus = flightStatus & 7;

                    switch (flightStatus)
                    {
                        case 0:
                            planeData.FlightStatus = "no alert, no SPI, aircraft is airborne";
                            break;
                        case 1:
                            planeData.FlightStatus = "no alert, no SPI, aircraft is on-ground";
                            break;
                        case 2:
                            planeData.FlightStatus = "alert, no SPI, aircraft is airborne";
                            break;
                        case 3:
                            planeData.FlightStatus = "alert, no SPI, aircraft is on-ground";
                            break;
                        case 4:
                            planeData.FlightStatus = "alert, SPI, aircraft is airborne or on-ground";
                            break;
                        case 5:
                            planeData.FlightStatus = "no alert, SPI, aircraft is airborne or on-ground";
                            break;
                        case 6:
                            planeData.FlightStatus = "reserved";
                            break;
                        case 7:
                            planeData.FlightStatus = "not assigned";
                            break;
                    }

                    int downlinkRequest = (int)ulong.Parse(hexstr.Substring(3, 1), System.Globalization.NumberStyles.HexNumber);
                    downlinkRequest = downlinkRequest >> 3;

                    switch (downlinkRequest)
                    {
                        case 0:
                            planeData.DownlinkRequest = "no downlink request";
                            break;
                        case 1:
                            planeData.DownlinkRequest = "request to send Comm-B message";
                            break;
                        case 4:
                            planeData.DownlinkRequest = "Comm-B broadcast message 1 available";
                            break;
                        case 5:
                            planeData.DownlinkRequest = "Comm-B broadcast message 2 available";
                            break;
                    }

                    int utilityMessage = (int)ulong.Parse(hexstr.Substring(3, 2), System.Globalization.NumberStyles.HexNumber);
                    utilityMessage = (utilityMessage >> 5) & 63;

                    switch (utilityMessage)
                    {
                        case 0:
                            planeData.DownlinkRequest = "no downlink request";
                            break;
                        case 1:
                            planeData.DownlinkRequest = "request to send Comm-B message";
                            break;
                        case 4:
                            planeData.DownlinkRequest = "Comm-B broadcast message 1 available";
                            break;
                        case 5:
                            planeData.DownlinkRequest = "Comm-B broadcast message 2 available";
                            break;
                    }

                    planeData = AddPlane();
                }
                if (planeData.Icao.Length >= 6) { return true; }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }

        }

        private string ConvertAsciiToHex(string hexstr)
        {
            char[] charValues = hexstr.ToCharArray();
            string hexOutput = "";
            foreach (char _eachChar in charValues)
            {
                // Get the integral value of the character.
                int value = Convert.ToInt32(_eachChar);
                // Convert the decimal value to a hexadecimal value in string form.
                hexOutput += String.Format("{0:X}", value).Replace("0x","");
                // to make output as your eg 
                //  hexOutput +=" "+ String.Format("{0:X}", value);

            }
            return hexOutput;
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
                if (planeData.Icao.Length==0)
                {
                    return null;
                }
                PlaneData match = listPlanes.Where(x => x.Icao.Contains(planeData.Icao)).FirstOrDefault();
                if (match == null)
                {

                    //planeData.Country = GetCountry(planeData.Icao);
                    //planeData.ModelName = GetModelName(planeData.Icao);
                    new Task(() => GetCountry(planeData.Icao)).Start();
                    new Task(() => GetModelName(planeData.Icao)).Start();
                    planeData.PackageReceiveTime = receiveTime;
                    planeData.TrackAngle = 0;
                    listPlanes.Add(planeData);
                    return listPlanes.Last();
                }
                else
                {
                    planeData.TimeSpan = receiveTime.Subtract(prevPacketTime).TotalSeconds;
                    int index = listPlanes.IndexOf(listPlanes.First(a => a.Icao.Contains(planeData.Icao)));
                    listPlanes[index].TimeSpan = planeData.TimeSpan;
                    if ( receiveTime.Subtract(planeData.LastCoordTime).TotalSeconds<120 && planeData.TimeSpanCoord<120 && planeData.Lat!=planeData.LatOld && planeData.Long!=planeData.LongOld && planeData.Lat!=-1 && planeData.Long!=-1 && planeData.LatOld != -1 && planeData.LongOld != -1)
                    {
                        planeData.TrackAngle = GetAzimuth(planeData.LatOld, planeData.LongOld, planeData.Lat, planeData.Long);
                    }//if the last coordinate was gotten not so far ago(less than 2 min) we take heading as angle between last two known coordinates(or current and previouse one) otherwise the track angle was taken as last heading or magnetic heading depending on last heading including message
                    
                    //foreach(var property in listPlanes[index].GetType().GetProperties())
                    //{ // Get the type and value of the property
                    //    Type type = property.PropertyType;
                    //    object value = property.GetValue(planeData);//value of the new data property

                    //    // Check if the value is a string
                    //    if (type == typeof(string))
                    //    {
                    //        // Check if the value is not empty
                    //        if ((string)value != "")
                    //        {
                    //            // Set the value of the property in listPlanes[index]
                    //            property.SetValue(listPlanes[index], value);//change old data of type string to new one
                    //        }
                    //    }
                    //    if(type==typeof(bool))
                    //    {
                    //        if ((bool)value != (bool)property.GetValue(listPlanes[index]))
                    //        {
                    //            // Set the value of the property in listPlanes[index]
                    //            property.SetValue(listPlanes[index], value);//change old data to new one
                    //        }
                    //    }
                    //    if (type == typeof(DateTime))
                    //    {
                    //        if ((DateTime)value != (DateTime)property.GetValue(listPlanes[index]))
                    //        {
                    //            // Set the value of the property in listPlanes[index]
                    //            property.SetValue(listPlanes[index], value);//change old data to new one
                    //        }
                    //    }
                    //    else
                    //    {
                    //        // Try to cast the value to double
                    //        double? number = value as double?;

                    //        // Check if the cast succeeded and the value is not -1
                    //        if (number.HasValue && number.Value != -1)
                    //        {
                    //            // Set the value of the property in listPlanes[index]
                    //            property.SetValue(listPlanes[index], value);
                    //        }
                    //    }

                    //}
                    return listPlanes[index];//data for some plane is updated
                }
            }
            catch(Exception ex)
            {
                return null;
            }
        }

        public PlaneData GetPlane()
        {
            try
            {
                PlaneData match = listPlanes.Where(x => x.Icao.Contains(planeData.Icao)).FirstOrDefault();
                if (match != null)
                {
                    return match;
                }
            }
            catch (Exception ex)
            {

            }
            return null;
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
            //if (planeData.TimeSpan >= 0 && planeData.TimeSpan <= 24 && planeData.CPRLatEven != -1 && planeData.CPRLongEven != -1 && planeData.Lat != -1 && planeData.Long != -1 && intF == 0)
            //{
            //    double dLat = 90 / (4 * 15);
            //    double j = Math.Floor(planeData.Lat / dLat) + Math.Floor(Mod(planeData.Lat, dLat) / dLat - planeData.CPRLatEven + 0.5);
            //    planeData.Lat = dLat * (j + planeData.CPRLatEven);

            //    double dLon = 90 / Math.Max(NL(planeData.Lat), 1);
            //    double m = Math.Floor(planeData.Long / dLon) + Math.Floor(Mod(planeData.Long, dLon) / dLon - planeData.CPRLongEven + 0.5);
            //    planeData.Long = dLon * (m + planeData.CPRLongEven);
            //}
            //even global position message
            //else
            //{
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


                        j = Math.Floor(59 * planeData.CPRLatEven - 60 * planeData.CPRLatOdd + 0.5);
                        planeData.LatOdd = 90 * (Mod(j, 59) + planeData.CPRLatOdd) / (4 * 15 - 1);

                        if (NL(planeData.LatEven) == NL(planeData.LatOdd))//the odd and even lat coord are in the same lat/long? zone otherwise we cant calculate lat
                        {
                            if (planeData.Hemisphere.Equals("Northern") || planeData.Hemisphere.Equals(""))//in the case of northern hemisphere of our(adsb receiver) location
                            {
                                planeData.Lat = planeData.LatEven;
                            }
                            else
                            {
                                planeData.Lat = planeData.Lat - 90;//in the case of southern hemisphere receiver location
                            }



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
            //}
            //odd latitude & long locally
            //if (planeData.TimeSpan >= 0 && planeData.TimeSpan <= 24 && planeData.CPRLatOdd != -1 && planeData.CPRLongOdd != -1 && planeData.Long != -1 && planeData.Lat != -1 && intF == 1)
            //{
            //    double dLat = 90 / (4 * 15 - 1);
            //    double j = Math.Floor(planeData.Lat / dLat) + Math.Floor(Mod(planeData.Lat, dLat) / dLat - planeData.CPRLatOdd + 0.5);
            //    planeData.Lat = dLat * (j + planeData.CPRLatOdd);

            //    double dLon = 90 / Math.Max(NL(planeData.Lat) - 1, 1);
            //    double m = Math.Floor(planeData.Long / dLon) + Math.Floor(Mod(planeData.Long, dLon) / dLon - planeData.CPRLongOdd + 0.5);
            //    planeData.Long = dLon * (m + planeData.CPRLongOdd);
            //}
            //odd global position message
            //else
            //{
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

                        j = Math.Floor(59 * planeData.CPRLatEven - 60 * planeData.CPRLatOdd + 0.5);
                        planeData.LatEven = 90 * (Mod(j, 60) + planeData.CPRLatEven) / (4 * 15);

                        if (NL(planeData.LatEven) == NL(planeData.LatOdd))//the odd and even lat coord are in the same lat zone otherwise we cant calculate lat
                        {

                            if (planeData.Hemisphere.Equals("Northern") || planeData.Hemisphere.Equals(""))//in the case of northern hemisphere of our(adsb receiver) location
                            {
                                planeData.Lat = planeData.LatEven;
                            }
                            else
                            {
                                planeData.Lat = planeData.Lat - 90;//in the case of southern hemisphere receiver location
                            }



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
            //}



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
                //planeData.Lat = planeData.Lat + 1;//for test
                string strF = hexstr.Substring(5, 1);
                int intF = int.Parse(strF, System.Globalization.NumberStyles.HexNumber);
                intF = (intF >> 2) & 1; 

                //Calculating time span between two odd and even messages
                
                //TimeSpan for Even message(if message is even)
                if(intF==0)
                {
                    planeData.PackageReceiveTimeEven = receiveTime;
                    if (planeData.PackageReceiveTimeOdd != DateTime.MinValue)
                    {
                        planeData.TimeSpanEven = receiveTime.Subtract(planeData.PackageReceiveTimeOdd).TotalSeconds;
                    }

                }

                //TimeSpan for Odd message(if message is Odd)
                if (intF == 1)
                {
                    planeData.PackageReceiveTimeOdd = receiveTime;
                    if (planeData.PackageReceiveTimeEven != DateTime.MinValue)
                    {
                        planeData.TimeSpanOdd = receiveTime.Subtract(planeData.PackageReceiveTimeEven).TotalSeconds;
                    }
                }

                //calculate acceptable distance for new coordinate if at least one coordinate was already known
                if (planeData.LastCoordTime != DateTime.MinValue)
                {
                    planeData.TimeSpanCoord = receiveTime.Subtract(planeData.LastCoordTime).TotalSeconds;                   
                    planeData.AcceptableDistance = planeData.TimeSpanCoord * 333;//acceptable new coordinates(position) radius from the last known postition in meteres, the speed taken is 333 meteres per second(1200kmh, not the last known speed) in timespan of equal or less than 5 minutes from the last position
                }
                //coordinates                
                //latitude and longitude globally(when previouse coordinate is unknown or further than 10sec) and locally(when previouse coordinate is known and calculated less then 10 sec before)

                //even latitude & long locally
                //if (planeData.IsLastGlobal==true && planeData.TimeSpanCoord >= 0 && planeData.TimeSpanCoord <= 15 && intF == 0 && planeData.Lat != -1 && planeData.Long != -1)
                //{
                //    string strCPRLat = hexstr.Substring(5, 5);
                //    long longCPRLat = long.Parse(strCPRLat, System.Globalization.NumberStyles.HexNumber);
                //    longCPRLat = (longCPRLat >> 1) & 131071;
                //    planeData.CPRLatEven = (double)longCPRLat / 131072;

                //    string strCPRLong = hexstr.Substring(9, 5);
                //    long longCPRLong = long.Parse(strCPRLong, System.Globalization.NumberStyles.HexNumber);
                //    longCPRLong = longCPRLong & 131071;
                //    planeData.CPRLongEven = (double)longCPRLong / 131072;

                //    double dLat = 360 / (4 * 15);
                //    double j = Math.Floor(planeData.Lat / dLat) + Math.Floor(Mod(planeData.Lat, dLat) / dLat - planeData.CPRLatEven + 0.5);
                //    planeData.Lat = dLat * (j + planeData.CPRLatEven);

                //    double dLon = 360 / Math.Max(NL(planeData.Lat), 1);
                //    double m = Math.Floor(planeData.Long/ dLon) + Math.Floor(Mod(planeData.Long, dLon) / dLon - planeData.CPRLongEven + 0.5); 
                //    planeData.Long = dLon * (m + planeData.CPRLongEven);

                //    planeData.LastCoordTime = receiveTime;

                //    planeData.IsLastGlobal = false;
                //}
                //even global position message
                //else
                //{
                    if (intF == 0)
                    {
                        if (planeData.TimeSpanEven >= 0 && planeData.TimeSpanEven <= 27 && planeData.CPRLatOdd != -1)
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
                            //planeData.LatEven = 360 * (j - 60 * Math.Floor(j / 60) + planeData.CPRLatEven) / (4 * 15);

                            j = Math.Floor(59 * planeData.CPRLatEven - 60 * planeData.CPRLatOdd + 0.5);
                            planeData.LatOdd = 360 * (Mod(j, 59) + planeData.CPRLatOdd) / (4 * 15 - 1);

                            if (planeData.LatEven >= 270)
                            {
                                planeData.LatEven = planeData.LatEven - 360;
                            }
                            if (NL(planeData.LatEven) == NL(planeData.LatOdd) && planeData.CPRLongOdd != -1)//the odd and even lat coord are in the same lat/long zone otherwise we cant calculate lat
                            {
                                if (planeData.Hemisphere.Equals("Northern") || planeData.Hemisphere.Equals(""))//in the case of northern hemisphere of our(adsb receiver) location
                                {
                                    planeData.Lat = planeData.LatEven;
                                }
                                if (planeData.Hemisphere.Equals("Southern"))
                                {
                                    planeData.Lat = planeData.Lat - 90;//in the case of southern hemisphere adsb receiver location
                                }




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

                                double dist = GetDistance(planeData.LatOld, planeData.LongOld, planeData.Lat, planeData.Long);
                            //check new coordinates for being valid
                            if ((planeData.AcceptableDistance != -1 && GetDistance(planeData.LatOld, planeData.LongOld, planeData.Lat, planeData.Long) <= planeData.AcceptableDistance) || (planeData.AcceptableDistance == 0 && GetDistance(planeData.LatOld, planeData.LongOld, planeData.Lat, planeData.Long) <= 4000))//planeData.AcceptableDistance!=0 - acceptable distance will be zero while both coordinates packets were in one packet to parse, so the time in acceptable dist calculation will be zero and the distance will be zero as a result 
                            {
                                planeData.LastCoordTime = receiveTime;
                            }
                            else if (planeData.AcceptableDistance != -1)//the coordinate is out of acceptable radius
                            {
                                planeData.Lat = planeData.LatOld;
                                planeData.Long = planeData.LongOld;
                            }
                            else if (planeData.AcceptableDistance == -1 && planeData.LastCoordTime == DateTime.MinValue)//we got the first current plane coordinates
                            {
                                planeData.LastCoordTime = receiveTime;
                            }




                            //planeData.IsLastGlobal = true;
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
                //}

                //odd latitude & long locally              
                //if (planeData.IsLastGlobal = true && planeData.TimeSpanCoord >= 0 && planeData.TimeSpanCoord <= 15 && intF == 1 && planeData.Lat!=-1 && planeData.Long!=-1)
                //{
                //    string strCPRLat = hexstr.Substring(5, 5);
                //    long longCPRLat = long.Parse(strCPRLat, System.Globalization.NumberStyles.HexNumber);
                //    longCPRLat = (longCPRLat >> 1) & 131071;
                //    planeData.CPRLatOdd = (double)longCPRLat / 131072;

                //    string strCPRLong = hexstr.Substring(9, 5);
                //    long longCPRLong = long.Parse(strCPRLong, System.Globalization.NumberStyles.HexNumber);
                //    longCPRLong = longCPRLong & 131071;
                //    planeData.CPRLongOdd = (double)longCPRLong / 131072;

                //    double dLat = 360 / (4 * 15 - 1);
                //    double j = Math.Floor(planeData.Lat / dLat) + Math.Floor(Mod(planeData.Lat, dLat) / dLat - planeData.CPRLatOdd + 0.5);
                //    planeData.Lat = dLat * (j + planeData.CPRLatOdd);

                //    double dLon = 360 / Math.Max(NL(planeData.Lat) - 1, 1);
                //    double m = Math.Floor(planeData.Long / dLon) + Math.Floor(Mod(planeData.Long, dLon) / dLon - planeData.CPRLongOdd + 0.5);
                //    planeData.Long = dLon * (m + planeData.CPRLongOdd);

                //    planeData.LastCoordTime = receiveTime;

                //    planeData.IsLastGlobal = false;
                //}
                //odd global position message
                //else
                //{
                    if (intF == 1)
                    {
                        if (planeData.TimeSpanOdd >= 0 && planeData.TimeSpanOdd <= 27 && planeData.CPRLatEven != -1)
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
                            //planeData.LatOdd = 360 * (j-59*Math.Floor(j/59) + planeData.CPRLatOdd) / (4 * 15 - 1);

                            j = Math.Floor(59 * planeData.CPRLatEven - 60 * planeData.CPRLatOdd + 0.5);
                            planeData.LatEven = 360 * (Mod(j, 60) + planeData.CPRLatEven) / (4 * 15);

                            if (planeData.LatOdd >= 270)
                            {
                                planeData.LatOdd = planeData.LatOdd - 360;
                            }
                            if (NL(planeData.LatEven) == NL(planeData.LatOdd) && planeData.CPRLongEven != -1)//the odd and even lat coord are in the same lat zone otherwise we cant calculate lat
                            {
                                if (planeData.Hemisphere.Equals("Northern") || planeData.Hemisphere.Equals(""))//in the case of northern hemisphere of our(adsb receiver) location
                                {
                                    planeData.Lat = planeData.LatEven;
                                }
                                if (planeData.Hemisphere.Equals("Southern"))
                                {
                                    planeData.Lat = planeData.Lat - 90;//in the case of southern hemisphere receiver location
                                }




                                ////calculate global longitude
                                double m = Math.Floor(planeData.CPRLongEven * (NL(planeData.Lat) - 1) - planeData.CPRLongOdd * NL(planeData.Lat) + 0.5);
                                double nOdd = Math.Max(NL(planeData.Lat) - 1, 1);
                                double dLonOdd = 360 / nOdd;
                                planeData.LongOdd = dLonOdd * (Mod(m, nOdd) + planeData.CPRLongOdd);
                                //planeData.LongOdd = dLonOdd * (m - nOdd * Math.Floor(m / nOdd) + planeData.CPRLongOdd);
                                if (planeData.LongOdd >= 180)
                                {
                                    planeData.LongOdd = planeData.LongOdd - 360;
                                }
                                planeData.Long = planeData.LongOdd;

                                double dist = GetDistance(planeData.LatOld, planeData.LongOld, planeData.Lat, planeData.Long);
                                //check new coordinates for being valid
                                if ((planeData.AcceptableDistance != -1 && GetDistance(planeData.LatOld, planeData.LongOld, planeData.Lat, planeData.Long) <= planeData.AcceptableDistance) || (planeData.AcceptableDistance == 0 && GetDistance(planeData.LatOld, planeData.LongOld, planeData.Lat, planeData.Long) <=4000 ))//planeData.AcceptableDistance!=0 - acceptable distance will be zero while both coordinates packets were in one packet to parse, so the time in acceptable dist calculation will be zero and the distance will be zero as a result 
                                {
                                    planeData.LastCoordTime = receiveTime;
                                }
                                else if (planeData.AcceptableDistance != -1)//the coordinate is out of acceptable radius
                                {
                                    planeData.Lat = planeData.LatOld;
                                    planeData.Long = planeData.LongOld;
                                }
                                else if (planeData.AcceptableDistance == -1 && planeData.LastCoordTime == DateTime.MinValue)//we got the first current plane coordinates
                                {
                                    planeData.LastCoordTime = receiveTime;
                                }


                                //planeData.IsLastGlobal = true;
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
                //}

                string strQ = hexstr.Substring(3, 1);
                int intQ = int.Parse(strQ, System.Globalization.NumberStyles.HexNumber);
                intQ = intQ & 1;

                string strAlt = hexstr.Substring(2, 3);
                int intAlt = int.Parse(strAlt, System.Globalization.NumberStyles.HexNumber);
                intAlt = ((intAlt & 4064) >> 1) | (intAlt & 15);
                
                if (intQ == 1)
                {
                    planeData.Altitude = (25 * intAlt - 1000) * 0.3048;//translate feets to meters *0.3048
                }

                if (intQ == 0)
                {
                    planeData.Altitude = (100 * intAlt - 1000) * 0.3048;//translate feets to meters *0.3048
                }
            }
            catch (Exception ex)
            {
                //planeData.Altitude = -1;
            }
        }
        private void ParseAirPosGNSS(string hexstr)
        {
            try
            {

                //planeData.Lat = planeData.Lat + 1;//for test
                string strF = hexstr.Substring(5, 1);
                int intF = int.Parse(strF, System.Globalization.NumberStyles.HexNumber);
                intF = (intF >> 2) & 1;

                //Calculating time span between two odd and even messages

                //TimeSpan for Even message(if message is even)
                if (intF == 0)
                {
                    planeData.PackageReceiveTimeEven = receiveTime;
                    if (planeData.PackageReceiveTimeOdd != DateTime.MinValue)
                    {
                        planeData.TimeSpanEven = receiveTime.Subtract(planeData.PackageReceiveTimeOdd).TotalSeconds;
                    }
                }

                //TimeSpan for Odd message(if message is Odd)
                if (intF == 1)
                {
                    planeData.PackageReceiveTimeOdd = receiveTime;
                    if (planeData.PackageReceiveTimeEven != DateTime.MinValue)
                    {
                        planeData.TimeSpanOdd = receiveTime.Subtract(planeData.PackageReceiveTimeEven).TotalSeconds;
                    }
                }

                if (planeData.LastCoordTime != DateTime.MinValue)
                {
                    planeData.TimeSpanCoord = receiveTime.Subtract(planeData.LastCoordTime).TotalSeconds;
                }
                //coordinates                
                //latitude and longitude globally(when previouse coordinate is unknown or further than 10sec) and locally(when previouse coordinate is known and calculated less then 10 sec before)

                //even latitude & long locally
                //if (planeData.IsLastGlobal == true && planeData.TimeSpanCoord >= 0 && planeData.TimeSpanCoord <= 15 && intF == 0 && planeData.Lat != -1 && planeData.Long != -1)
                //{
                //    string strCPRLat = hexstr.Substring(5, 5);
                //    long longCPRLat = long.Parse(strCPRLat, System.Globalization.NumberStyles.HexNumber);
                //    longCPRLat = (longCPRLat >> 1) & 131071;
                //    planeData.CPRLatEven = (double)longCPRLat / 131072;

                //    string strCPRLong = hexstr.Substring(9, 5);
                //    long longCPRLong = long.Parse(strCPRLong, System.Globalization.NumberStyles.HexNumber);
                //    longCPRLong = longCPRLong & 131071;
                //    planeData.CPRLongEven = (double)longCPRLong / 131072;

                //    double dLat = 360 / (4 * 15);
                //    double j = Math.Floor(planeData.Lat / dLat) + Math.Floor(Mod(planeData.Lat, dLat) / dLat - planeData.CPRLatEven + 0.5);
                //    planeData.Lat = dLat * (j + planeData.CPRLatEven);

                //    double dLon = 360 / Math.Max(NL(planeData.Lat), 1);
                //    double m = Math.Floor(planeData.Long / dLon) + Math.Floor(Mod(planeData.Long, dLon) / dLon - planeData.CPRLongEven + 0.5);
                //    planeData.Long = dLon * (m + planeData.CPRLongEven);

                //    planeData.LastCoordTime = receiveTime;

                //    planeData.IsLastGlobal = false;
                //}
                //even global position message
                //else
                //{
                    if (intF == 0)
                    {
                        if (planeData.TimeSpanEven >= 0 && planeData.TimeSpanEven <= 27 && planeData.CPRLatOdd != -1)
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
                            //planeData.LatEven = 360 * (j - 60 * Math.Floor(j / 60) + planeData.CPRLatEven) / (4 * 15);

                            j = Math.Floor(59 * planeData.CPRLatEven - 60 * planeData.CPRLatOdd + 0.5);
                            planeData.LatOdd = 360 * (Mod(j, 59) + planeData.CPRLatOdd) / (4 * 15 - 1);

                            if (planeData.LatEven >= 270)
                            {
                                planeData.LatEven = planeData.LatEven - 360;
                            }
                            if (NL(planeData.LatEven) == NL(planeData.LatOdd) && planeData.CPRLongOdd != -1)//the odd and even lat coord are in the same lat/long zone otherwise we cant calculate lat
                            {
                                if (planeData.Hemisphere.Equals("Northern") || planeData.Hemisphere.Equals(""))//in the case of northern hemisphere of our(adsb receiver) location
                                {
                                    planeData.Lat = planeData.LatEven;
                                }
                                if (planeData.Hemisphere.Equals("Southern"))
                                {
                                    planeData.Lat = planeData.Lat - 90;//in the case of southern hemisphere receiver location
                                }




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

                            //check new coordinates for being valid                           
                            if ((planeData.AcceptableDistance != -1 && GetDistance(planeData.LatOld, planeData.LongOld, planeData.Lat, planeData.Long) <= planeData.AcceptableDistance) || (planeData.AcceptableDistance == 0 && GetDistance(planeData.LatOld, planeData.LongOld, planeData.Lat, planeData.Long) <= 4000))//planeData.AcceptableDistance!=0 - acceptable distance will be zero while both coordinates packets were in one packet to parse, so the time in acceptable dist calculation will be zero and the distance will be zero as a result 
                            {
                                planeData.LastCoordTime = receiveTime;
                            }
                            else if (planeData.AcceptableDistance != -1)//the coordinate is out of acceptable radius so the last coordinates are the previous one
                            {
                                planeData.Lat = planeData.LatOld;
                                planeData.Long = planeData.LongOld;
                            }
                            else if (planeData.AcceptableDistance == -1 && planeData.LastCoordTime == DateTime.MinValue)//we got the first current plane coordinates
                            {
                                planeData.LastCoordTime = receiveTime;
                            }



                            //planeData.IsLastGlobal = true;
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
                //}

                //odd latitude & long locally              
                //if (planeData.IsLastGlobal = true && planeData.TimeSpanCoord >= 0 && planeData.TimeSpanCoord <= 15 && intF == 1 && planeData.Lat != -1 && planeData.Long != -1)
                //{
                //    string strCPRLat = hexstr.Substring(5, 5);
                //    long longCPRLat = long.Parse(strCPRLat, System.Globalization.NumberStyles.HexNumber);
                //    longCPRLat = (longCPRLat >> 1) & 131071;
                //    planeData.CPRLatOdd = (double)longCPRLat / 131072;

                //    string strCPRLong = hexstr.Substring(9, 5);
                //    long longCPRLong = long.Parse(strCPRLong, System.Globalization.NumberStyles.HexNumber);
                //    longCPRLong = longCPRLong & 131071;
                //    planeData.CPRLongOdd = (double)longCPRLong / 131072;

                //    double dLat = 360 / (4 * 15 - 1);
                //    double j = Math.Floor(planeData.Lat / dLat) + Math.Floor(Mod(planeData.Lat, dLat) / dLat - planeData.CPRLatOdd + 0.5);
                //    planeData.Lat = dLat * (j + planeData.CPRLatOdd);

                //    double dLon = 360 / Math.Max(NL(planeData.Lat) - 1, 1);
                //    double m = Math.Floor(planeData.Long / dLon) + Math.Floor(Mod(planeData.Long, dLon) / dLon - planeData.CPRLongOdd + 0.5);
                //    planeData.Long = dLon * (m + planeData.CPRLongOdd);

                //    planeData.LastCoordTime = receiveTime;

                //    planeData.IsLastGlobal = false;
                //}
                //odd global position message
                //else
                //{
                    if (intF == 1)
                    {
                        if (planeData.TimeSpanOdd >= 0 && planeData.TimeSpanOdd <= 27 && planeData.CPRLatEven != -1)
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
                            //planeData.LatOdd = 360 * (j-59*Math.Floor(j/59) + planeData.CPRLatOdd) / (4 * 15 - 1);
                            
                            j = Math.Floor(59 * planeData.CPRLatEven - 60 * planeData.CPRLatOdd + 0.5);
                            planeData.LatEven = 360 * (Mod(j, 60) + planeData.CPRLatEven) / (4 * 15);

                            if (planeData.LatOdd >= 270)
                            {
                                planeData.LatOdd = planeData.LatOdd - 360;
                            }
                            if (NL(planeData.LatEven) == NL(planeData.LatOdd) && planeData.CPRLongEven != -1)//the odd and even lat coord are in the same lat zone otherwise we cant calculate lat
                            {
                                if (planeData.Hemisphere.Equals("Northern") || planeData.Hemisphere.Equals(""))//in the case of northern hemisphere of our(adsb receiver) location
                                {
                                    planeData.Lat = planeData.LatEven;
                                }
                                if (planeData.Hemisphere.Equals("Southern"))
                                {
                                    planeData.Lat = planeData.Lat - 90;//in the case of southern hemisphere receiver location
                                }




                                ////calculate global longitude
                                double m = Math.Floor(planeData.CPRLongEven * (NL(planeData.Lat) - 1) - planeData.CPRLongOdd * NL(planeData.Lat) + 0.5);
                                double nOdd = Math.Max(NL(planeData.Lat) - 1, 1);
                                double dLonOdd = 360 / nOdd;
                                planeData.LongOdd = dLonOdd * (Mod(m, nOdd) + planeData.CPRLongOdd);
                                //planeData.LongOdd = dLonOdd * (m - nOdd * Math.Floor(m / nOdd) + planeData.CPRLongOdd);
                                if (planeData.LongOdd >= 180)
                                {
                                    planeData.LongOdd = planeData.LongOdd - 360;
                                }
                                planeData.Long = planeData.LongOdd;

                            //check new coordinates for being valid                           
                            if (GetDistance(planeData.LatOld, planeData.LongOld, planeData.Lat, planeData.Long) <= planeData.AcceptableDistance)
                            {
                                planeData.LastCoordTime = receiveTime;
                            }
                            else
                            {
                                planeData.Lat = planeData.LatOld;
                                planeData.Long = planeData.LongOld;
                            }

                            //planeData.IsLastGlobal = true;
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
                //}

                string strAlt = hexstr.Substring(2, 3);
                int intAlt = int.Parse(strAlt, System.Globalization.NumberStyles.HexNumber);
                planeData.Altitude = intAlt;//in meters by default
            }
            catch (Exception ex)
            {

            }
        }

        private void ParseAirVelocity(string hexstr)
        {
            string strSubType = hexstr.Substring(0, 2);
            int intSubType = int.Parse(strSubType, System.Globalization.NumberStyles.HexNumber);
            intSubType = intSubType & 7;

            string strIC = hexstr.Substring(2, 1);
            int intIC = int.Parse(strIC, System.Globalization.NumberStyles.HexNumber);
            intIC = intIC >> 3;
            planeData.IC = intIC;

            string strIFR = hexstr.Substring(2, 1);
            int intIFR = int.Parse(strIFR, System.Globalization.NumberStyles.HexNumber);
            intIFR = (intIFR >> 2) & 1;
            planeData.IFR = intIFR;

            string strNUCr = hexstr.Substring(2, 2);
            int intNUCr = int.Parse(strNUCr, System.Globalization.NumberStyles.HexNumber);
            intNUCr = (intNUCr >> 3) & 7;
            planeData.NUСr = intNUCr;

            ///parse subtypes
            ///if (intSubType == 0) {no speed data}
            if (intSubType == 1)
            {
                string strDew = hexstr.Substring(3, 1);
                int intDew = int.Parse(strDew, System.Globalization.NumberStyles.HexNumber);
                intDew = (intDew >> 2) & 1;
                planeData.Dew = intDew;

                string strDns = hexstr.Substring(6, 1);
                int intDns = int.Parse(strDns, System.Globalization.NumberStyles.HexNumber);
                intDns = intDns >> 3;
                planeData.Dns = intDns;

                if (intDew == 0)
                {
                    string strSpeedWE = hexstr.Substring(3, 3);
                    int intSpeedWE = int.Parse(strSpeedWE, System.Globalization.NumberStyles.HexNumber);
                    intSpeedWE = intSpeedWE & 1023;
                    if (intSpeedWE != 0)
                    {
                        planeData.WestToEastSpeed = intSpeedWE - 1;
                        planeData.EastToWestSpeed = -planeData.WestToEastSpeed;
                    }

                }

                if (intDew == 1)
                {
                    string strSpeedEW = hexstr.Substring(3, 3);
                    int intSpeedEW = int.Parse(strSpeedEW, System.Globalization.NumberStyles.HexNumber);
                    intSpeedEW = intSpeedEW & 1023;
                    if (intSpeedEW != 0)
                    {
                        planeData.EastToWestSpeed = (-1) * (intSpeedEW - 1);
                        planeData.WestToEastSpeed = -planeData.EastToWestSpeed;
                    }
                }

                if (intDns == 0)
                {
                    string strSpeedSN = hexstr.Substring(6, 3);
                    int intSpeedSN = int.Parse(strSpeedSN, System.Globalization.NumberStyles.HexNumber);
                    intSpeedSN = (intSpeedSN & 2047)>>1;
                    if (intSpeedSN != 0)
                    {
                        planeData.SouthToNorthSpeed = intSpeedSN - 1;
                        planeData.NorthToSouthSpeed = -planeData.SouthToNorthSpeed;
                    }

                }

                if (intDns == 1)
                {
                    string strSpeedNS = hexstr.Substring(6, 3);
                    int intSpeedNS = int.Parse(strSpeedNS, System.Globalization.NumberStyles.HexNumber);
                    intSpeedNS = (intSpeedNS & 2047)>>1;
                    if (intSpeedNS != 0)
                    {
                        planeData.NorthToSouthSpeed = (-1) * (intSpeedNS - 1);
                        planeData.SouthToNorthSpeed = -planeData.NorthToSouthSpeed;
                    }
                }

                planeData.AirSpeed = Math.Sqrt(Math.Pow(planeData.WestToEastSpeed, 2) + Math.Pow(planeData.SouthToNorthSpeed, 2));
                if (intDew == 0)
                {
                    if (intDns == 0)
                    {
                        planeData.Heading = Mod(Math.Atan2(planeData.WestToEastSpeed, planeData.SouthToNorthSpeed) * 360 / (2 * Math.PI), 360);
                        planeData.TrackAngle = planeData.Heading;
                    }
                    if (intDns == 1)
                    {
                        planeData.Heading = Mod(Math.Atan2(planeData.WestToEastSpeed, planeData.NorthToSouthSpeed) * 360 / (2 * Math.PI), 360);
                        planeData.TrackAngle = planeData.Heading;
                    }
                }
                if (intDew == 1)
                {
                    if (intDns == 0)
                    {
                        planeData.Heading = Mod(Math.Atan2(planeData.EastToWestSpeed, planeData.SouthToNorthSpeed) * 360 / (2 * Math.PI), 360);
                        planeData.TrackAngle = planeData.Heading;
                    }
                    if (intDns == 1)
                    {
                        planeData.Heading = Mod(Math.Atan2(planeData.EastToWestSpeed, planeData.NorthToSouthSpeed) * 360 / (2 * Math.PI), 360);
                        planeData.TrackAngle = planeData.Heading;
                    }
                }

            }
            if (intSubType == 2)
            {
                string strDew = hexstr.Substring(3, 1);
                int intDew = int.Parse(strDew, System.Globalization.NumberStyles.HexNumber);
                intDew = (intDew >> 2) & 1;
                planeData.Dew = intDew;

                string strDns = hexstr.Substring(6, 1);
                int intDns = int.Parse(strDns, System.Globalization.NumberStyles.HexNumber);
                intDns = intDns >> 3;
                planeData.Dns = intDns;

                if (intDew == 0)
                {
                    string strSpeedWE = hexstr.Substring(3, 3);
                    int intSpeedWE = int.Parse(strSpeedWE, System.Globalization.NumberStyles.HexNumber);
                    intSpeedWE = intSpeedWE & 1023;
                    if (intSpeedWE != 0)
                    {
                        planeData.WestToEastSpeed = 4 * (intSpeedWE - 1);
                        planeData.EastToWestSpeed = -planeData.WestToEastSpeed;
                    }
                }

                if (intDew == 1)
                {
                    string strSpeedEW = hexstr.Substring(3, 3);
                    int intSpeedEW = int.Parse(strSpeedEW, System.Globalization.NumberStyles.HexNumber);
                    intSpeedEW = intSpeedEW & 1023;
                    if (intSpeedEW != 0)
                    {
                        planeData.EastToWestSpeed = (-4) * (intSpeedEW - 1);
                        planeData.WestToEastSpeed = -planeData.EastToWestSpeed;
                    }
                }

                if (intDns == 0)
                {
                    string strSpeedSN = hexstr.Substring(6, 3);
                    int intSpeedSN = int.Parse(strSpeedSN, System.Globalization.NumberStyles.HexNumber);
                    intSpeedSN = (intSpeedSN & 2047) >> 1;
                    if (intSpeedSN != 0)
                    {
                        planeData.SouthToNorthSpeed = 4 * (intSpeedSN - 1);
                        planeData.NorthToSouthSpeed = -planeData.SouthToNorthSpeed;
                    }
                }

                if (intDns == 1)
                {
                    string strSpeedNS = hexstr.Substring(6, 3);
                    int intSpeedNS = int.Parse(strSpeedNS, System.Globalization.NumberStyles.HexNumber);
                    intSpeedNS = (intSpeedNS & 2047) >> 1;
                    if (intSpeedNS != 0)
                    {
                        planeData.NorthToSouthSpeed = (-4) * (intSpeedNS - 1);
                        planeData.SouthToNorthSpeed = -planeData.NorthToSouthSpeed;
                    }
                }

                planeData.AirSpeed = Math.Sqrt(Math.Pow(planeData.WestToEastSpeed, 2) + Math.Pow(planeData.SouthToNorthSpeed, 2));
                if (intDew == 0)
                {
                    if (intDns == 0)
                    {
                        planeData.Heading = Mod(Math.Atan2(planeData.WestToEastSpeed, planeData.SouthToNorthSpeed) * 360 / (2 * Math.PI), 360);
                        planeData.TrackAngle = planeData.Heading;
                    }
                    if (intDns == 1)
                    {
                        planeData.Heading = Mod(Math.Atan2(planeData.WestToEastSpeed, planeData.NorthToSouthSpeed) * 360 / (2 * Math.PI), 360);
                        planeData.TrackAngle = planeData.Heading;
                    }
                }
                if (intDew == 1)
                {
                    if (intDns == 0)
                    {
                        planeData.Heading = Mod(Math.Atan2(planeData.EastToWestSpeed, planeData.SouthToNorthSpeed) * 360 / (2 * Math.PI), 360);
                        planeData.TrackAngle = planeData.Heading;
                    }
                    if (intDns == 1)
                    {
                        planeData.Heading = Mod(Math.Atan2(planeData.EastToWestSpeed, planeData.NorthToSouthSpeed) * 360 / (2 * Math.PI), 360);
                        planeData.TrackAngle = planeData.Heading;
                    }
                }
            }
            if (intSubType == 3)
            {
                string strSH = hexstr.Substring(3, 1);
                int intSH = int.Parse(strSH, System.Globalization.NumberStyles.HexNumber);
                intSH = (intSH >> 2) & 1;
                planeData.SH = intSH;

                string strT = hexstr.Substring(6, 1);
                int intT = int.Parse(strT, System.Globalization.NumberStyles.HexNumber);
                intT = intT >> 3;
                planeData.AirSpeedType = intT;

                if (planeData.AirSpeedType == 0)
                {
                    planeData.AirSpeedTypeName = "Indicated airspeed, IAS";
                }
                if (planeData.AirSpeedType == 1)
                {
                    planeData.AirSpeedTypeName = "True airspeed, TAS";
                }

                if (planeData.SH == 1)//1->message contains magnetic heading information
                {
                    string strHDG = hexstr.Substring(3, 3);
                    int intHDG = int.Parse(strHDG, System.Globalization.NumberStyles.HexNumber);
                    intHDG = intHDG & 1023;

                    planeData.MagneticHeading = intHDG * 360 / 1024;
                    planeData.TrackAngle = planeData.MagneticHeading;
                }

                string strAS = hexstr.Substring(6, 3);
                int intAS = int.Parse(strAS, System.Globalization.NumberStyles.HexNumber);
                intAS = (intAS & 2047)>>1;

                planeData.AirSpeed = intAS - 1;


            }
            if (intSubType == 4)
            {
                string strSH = hexstr.Substring(3, 1);
                int intSH = int.Parse(strSH, System.Globalization.NumberStyles.HexNumber);
                intSH = (intSH >> 2) & 1;
                planeData.SH = intSH;

                string strT = hexstr.Substring(6, 1);
                int intT = int.Parse(strT, System.Globalization.NumberStyles.HexNumber);
                intT = intT >> 3;
                planeData.AirSpeedType = intT;

                if (planeData.AirSpeedType == 0)
                {
                    planeData.AirSpeedTypeName = "Indicated airspeed, IAS";
                }
                if (planeData.AirSpeedType == 1)
                {
                    planeData.AirSpeedTypeName = "True airspeed, TAS";
                }

                if (planeData.SH == 1)//1->message contains magnetic heading information
                {
                    string strHDG = hexstr.Substring(3, 3);
                    int intHDG = int.Parse(strHDG, System.Globalization.NumberStyles.HexNumber);
                    intHDG = intHDG & 1023;

                    planeData.MagneticHeading = intHDG * 360 / 1024;
                    planeData.TrackAngle = planeData.MagneticHeading;
                }

                string strAS = hexstr.Substring(6, 3);
                int intAS = int.Parse(strAS, System.Globalization.NumberStyles.HexNumber);
                intAS = (intAS & 2047)>>1;

                planeData.AirSpeed = 4 * (intAS - 1);
            }
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
        }

        StringBuilder errorSyndrome = new StringBuilder();
        string polynom = "1111111111111010000001001";//25 bits divisor polynom(the same as in the instruction book "book-the_1090mhz_riddle-junzi_sun.pdf")
        public bool CheckCRC(string hexstr, int downlinkFormat)
        {
            string binarystring = String.Join(String.Empty, hexstr.Select(c => Convert.ToString(Convert.ToInt32(c.ToString(), 16), 2).PadLeft(4, '0')));//переводим 16 ичные данные пакета в их двоичный вид
            StringBuilder sbbinarystring = new StringBuilder(binarystring);

            //Parity type 1 only df17 and df18
            if (downlinkFormat == 17 || downlinkFormat == 18)
            {
                //calculating crc of 112 bits received packet, should be zero if no errors have occured during transmission
                for (int i = 0; i <= sbbinarystring.Length - 1 - 24; i++)
                {

                    if (sbbinarystring[i].Equals('1'))
                    {
                        for (int j = 0; j < polynom.Length; j++)
                        {

                            if (sbbinarystring[i + j].Equals(polynom[j])) { sbbinarystring[i + j] = '0'; }
                            else sbbinarystring[i + j] = '1';

                           

                        }
                    }
                }

                errorSyndrome = new StringBuilder();
                //writing crc remainder(if has ones its also error syndrome)
                for (int i = sbbinarystring.Length - 1 - 23; i <= sbbinarystring.Length - 1; i++)
                {
                    errorSyndrome.Append(sbbinarystring[i]);
                }

                //if remainder(last 24 bits) have any bit which is equal to '1' there was an error in the message
                for (int i = sbbinarystring.Length - 1 - 23; i <= sbbinarystring.Length - 1; i++)
                {
                    if (sbbinarystring[i].Equals('1'))
                    {
                        return false;//at least one bit from remainder isnt equal to 0 so the crc isnt equal to zero
                    }
                }
            }
            //Parity type 2 Address Parity(all other packets)
            //else
            //{

            //}
            //Parity type 3 Data Parity(some? df20 and df21 packets)
            if(downlinkFormat == 20 || downlinkFormat == 21)
            {
                string strBdsCode = "";
                //calculating crc of 112 bits received packet, should be zero if no errors have occured during transmission
                for (int i = 0; i <= sbbinarystring.Length - 1 - 24; i++)
                {

                    if (sbbinarystring[i].Equals('1'))
                    {
                        for (int j = 0; j < polynom.Length; j++)
                        {

                            if (sbbinarystring[i + j].Equals(polynom[j])) { sbbinarystring[i + j] = '0'; }
                            else sbbinarystring[i + j] = '1';

                        }
                    }
                }

                string ICAO = "";
                string strBinaryIcao = sbbinarystring.ToString();
                strBinaryIcao = strBinaryIcao.Substring(sbbinarystring.Length - 1 - 23, 24);

                ICAO = BinaryStringToHexString(strBinaryIcao);
                if(isKnownIcao(ICAO) == false)
                {
                    return false;
                }
                planeData.Icao = ICAO;

               

                //TODO: check by calculated icao if the plane with such icao exists maybe using csv opensky planes database 

                //next the parsing of df 20 message body data
                
                string strHeadingStatus = hexstr.Substring(0, 1);
                int intHeadingStatus = int.Parse(strHeadingStatus, System.Globalization.NumberStyles.HexNumber);
                intHeadingStatus = intHeadingStatus >> 3;

                string strSign = hexstr.Substring(0,1);
                int intSign = int.Parse(strSign, System.Globalization.NumberStyles.HexNumber);
                intSign = (intSign >> 2) & 1;//0 is plus or 1 is minus before heading value

                if (intHeadingStatus == 1)//double complementary encoded heading
                {
                   //дописать
                }
                else
                {
                    string strHeading = hexstr.Substring(0, 3);
                    int intHeading = int.Parse(strHeading, System.Globalization.NumberStyles.HexNumber);
                    if (intSign == 0)
                    {
                        planeData.TrackAngle = (intHeading & 1023) * 90 / 512; //heading in degrees
                    }
                    else
                    {
                        planeData.TrackAngle = -(intHeading & 1023) * 90 / 512; //heading in degrees
                    }
                }


            }
            return true;
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

        ///Comm-B message parsers next
        ///
        private void ParseBDS60(string hexstr)
        {
            ///change it to correct parser 31072023
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
        }

        /// <summary>
        /// Additional methods used in some calculations
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        private double Mod(double x, double y)
        {
            if (y != 0)
            {
                return x - (int)y * Math.Floor(x / y);
                //return (Math.Abs(x * y) + x) % y;
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

        private string BinaryStringToHexString(string binary)
        {
            if (string.IsNullOrEmpty(binary))
                return binary;

            StringBuilder result = new StringBuilder(binary.Length / 8 + 1);

            // TODO: check all 1's or 0's... throw otherwise

            int mod4Len = binary.Length % 8;
            if (mod4Len != 0)
            {
                // pad to length multiple of 8
                binary = binary.PadLeft(((binary.Length / 8) + 1) * 8, '0');
            }

            for (int i = 0; i < binary.Length; i += 8)
            {
                string eightBits = binary.Substring(i, 8);
                result.AppendFormat("{0:X2}", Convert.ToByte(eightBits, 2));
            }

            return result.ToString();
        }      

        public void GetCountry(string icaoValue)
        {
            try
            {
                HtmlDocument htmlDoc = new HtmlDocument();

                // filePath is a path to a file containing the html
                //htmlDoc.Load(Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.Parent.FullName + "\\ATDB - ICAO 24-bit addresses - Decode.html");
                htmlDoc.Load(System.AppDomain.CurrentDomain.BaseDirectory + "AdsbDocuments\\ATDB - ICAO 24-bit addresses - Decode.html");
                //htmlDoc.Load(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "Documents\\ATDB - ICAO 24-bit addresses - Decode.html");
                HtmlNodeCollection coll = htmlDoc.DocumentNode.SelectNodes("//text()");

                int icaoIntValue = Convert.ToInt32(icaoValue, 16);
                for (int i = 0; i <= coll.Count; i++)
                {
                    try
                    {
                        if (Convert.ToInt32(coll[i].InnerText.ToString(), 16) < icaoIntValue)
                        {
                            if (Convert.ToInt32(coll[i + 2].InnerText.ToString(), 16) > icaoIntValue)
                            {
                                //try
                                //{
                                //    planeData.Flag = coll[i + 4].InnerText.ToString() + ".png";//Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.Parent.FullName + "\\CountryFlags\\" + coll[i + 4].InnerText.ToString() + ".png";
                                //}
                                //catch (Exception ex)
                                //{

                                //}
                                //return coll[i + 4].InnerText.ToString();
                                PlaneData match = listPlanes.Where(x => x.Icao.Contains(icaoValue)).FirstOrDefault();
                                if (match != null)
                                {                                   
                                    try
                                    {
                                        match.Country = coll[i + 4].InnerText.ToString();
                                        match.Flag = coll[i + 4].InnerText.ToString() + ".png";//Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.Parent.FullName + "\\CountryFlags\\" + coll[i + 4].InnerText.ToString() + ".png";
                                    }
                                    catch (Exception ex)
                                    {

                                    }
                                }
                                else
                                {
                                    match.Country = "unknown";
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }
            catch(Exception ex)
            {

            }

            //return "unknown";
        }

        /// <summary>
        /// find planes producer name and plane model
        /// </summary>
        /// <param name="icaoValue"></param>
        /// <returns></returns>
        //public string GetModelName(string icaoValue)
        //{
        //    try
        //    {
        //        string filePath = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.Parent.FullName + "\\aircraft-database-complete-2023-09-2.csv";

        //        using (ExcelPackage package = new ExcelPackage(new FileInfo(filePath)))
        //        {
        //            ExcelWorksheet worksheet = package.Workbook.Worksheets[0];

        //            int rowCount = worksheet.Dimension.Rows;
        //            int colCount = worksheet.Dimension.Columns;

        //            for (int row = 1; row <= rowCount; row++)
        //            {
        //                // Get the first column value as a string
        //                string firstColumn = worksheet.Cells[row, 1].Value.ToString();

        //                // Check if it matches the search value
        //                if (firstColumn.ToLower().Equals(icaoValue.ToLower()))
        //                {
        //                    // Get the 4th and 5th elements from the first column as strings
        //                    string fourthElement = firstColumn.Split(',')[2].Length==0?firstColumn.Split(',')[3]:firstColumn.Split(',')[2];
        //                    string fifthElement = firstColumn.Split(',')[4];

        //                    return fourthElement + " " + fifthElement;
        //                }
        //            }
        //        }
        //    }
        //    catch(Exception ex)
        //    {
        //        return null;
        //    }
        //    return "unknown";

        //}

        public void GetModelName(string icaoValue)
        {
            try
            {
                //string filePath = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.Parent.FullName + "\\aircraft-database-complete-2023-09.csv";
                //string filePath = Directory.GetParent(Environment.CurrentDirectory).FullName + "\\Documents\\aircraft-database-complete-2024-06.csv";
                string filePath = (System.AppDomain.CurrentDomain.BaseDirectory + "AdsbDocuments\\aircraft-database-complete-2024-06.csv");
                // Create a new TextFieldParser object
                using (TextFieldParser parser = new TextFieldParser(filePath))
                {
                    // Set the delimiter to comma
                    parser.SetDelimiters(",");


                    // Skip the first 7 rows
                    for (int i = 0; i < 7; i++)
                    {
                        parser.ReadLine();
                    }

               

                    // Loop through the remaining rows
                    while (!parser.EndOfData)
                    {
                        try
                        {
                            // Read the fields of the current row
                            string[] fields = parser.ReadFields();

                            //if icao inside excel doc is  enclosed within single quotes ('') we should remove them
                            fields[0] = fields[0].Replace("'","");
                            if(parser.LineNumber==98002)
                            {

                            }
                            // Check if the first field matches the value
                            if (fields[0].ToLower().Equals(icaoValue.ToLower()))
                            {
                                //string fourthElement = fields[2].Length == 0 ? fields[3] : fields[2];
                                //string fifthElement = fields[4];
                                //string Manufacturer = fields[12];
                                string ModelName = fields[14];
                                //return fourthElement + " " + fifthElement;
                                //return Manufacturer + " " + ModelName;
                                PlaneData match = listPlanes.Where(x => x.Icao.Contains(icaoValue)).FirstOrDefault();
                                if (match != null)
                                {
                                    try
                                    {
                                        //match.ModelName = Manufacturer + " " + ModelName;
                                        match.ModelName = ModelName;
                                    }
                                    catch (Exception ex)
                                    {

                                    }
                                }
                                else
                                {
                                    match.ModelName = "unknown";
                                }
                                //break;
                            }
                        }
                        catch(Exception ex)
                        {

                        }
                    }
                }



            }
            catch (Exception ex)
            {
                //return null;
            }
            //return "unknown";

        }
        public bool isKnownIcao(string ICAO)
        {
            PlaneData match = listPlanes.Where(x => x.Icao.Contains(ICAO)).FirstOrDefault();
            if (match == null)
            {
                return false;
            }
            else return true;
        }

        // Define a method for calculating the azimuth between two coordinates
        public static double GetAzimuth(double oldLat, double oldLon, double newLat, double newLon)
        {
            // Convert degrees to radians
            oldLat = oldLat * Math.PI / 180;
            oldLon = oldLon * Math.PI / 180;
            newLat = newLat * Math.PI / 180;
            newLon = newLon * Math.PI / 180;

            // Calculate the difference in longitude
            double deltaLon = newLon - oldLon;

            // Calculate the azimuth using the formula from [1]
            double y = Math.Sin(deltaLon) * Math.Cos(newLat);
            double x = Math.Cos(oldLat) * Math.Sin(newLat) - Math.Sin(oldLat) * Math.Cos(newLat) * Math.Cos(deltaLon);
            double azimuth = Math.Atan2(y, x) * 180 / Math.PI; // in degrees

            // Normalize the azimuth to [0, 360] range
            if (azimuth < 0)
            {
                azimuth += 360;
            }

            return azimuth;
        }

        /// <summary>
        /// Method to calculate distance between to coordinates on Earth
        /// </summary>
        /// <param name="lat1"></param>
        /// <param name="lon1"></param>
        /// <param name="lat2"></param>
        /// <param name="lon2"></param>
        /// <returns></returns>
        public static double GetDistance(double lat1, double lon1, double lat2, double lon2)
        {
            // The radius of the earth in kilometers
            const double R = 6371;

            // Convert the coordinates to radians
            lat1 = ToRadians(lat1);
            lon1 = ToRadians(lon1);
            lat2 = ToRadians(lat2);
            lon2 = ToRadians(lon2);

            // Calculate the differences between the coordinates
            double dLat = lat2 - lat1;
            double dLon = lon2 - lon1;

            // Apply the Haversine formula
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1) * Math.Cos(lat2) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            double d = R * c;

            // Return the distance in meters
            return d*1000;
        }

        /// <summary>
        /// A method to convert degrees to radians
        /// </summary>
        /// <param name="degrees"></param>
        /// <returns></returns>
        static double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }
        Dictionary<BigInteger, BigInteger> syndromesDict = new Dictionary<BigInteger, BigInteger>();

        //вычисление всех векторов ошибок и их crc для 1 ошибки в 88 битном теле сообщения
        private void CalcPermutations1(int vectorLength)
        {
            try
            {
                StringBuilder bitsList = new StringBuilder();
                for (int m = 0; m < vectorLength; m++)
                {
                    bitsList.Append('0'); // Initialize starting sequence with each bite equal to zero
                }
                StringBuilder ending = new StringBuilder(24);
                for (int m = 0; m < 24; m++)
                {
                    ending.Append('0');
                }
                bitsList.Append(ending);//добавили нулями до 112 бит, чтобы далее можно было вычислить crc от конкретного вектора ошибки
                for (int i = 0; i < 88; i++)
                {

                    if (bitsList[i].Equals('1'))
                    {
                        continue;
                    }
                    bitsList[i] = '1';//задаём определенный ошибочный бит в векторе ошибок и далее вычисляем его crc(синдром данной ошибки)

                    StringBuilder bitsListMsg = new StringBuilder(bitsList.Length);
                    bitsListMsg.Append(bitsList);
                    BigInteger sequenceValue = bitsListMsg.Remove(88, 24).ToString().Aggregate(BigInteger.Zero, (s, a) => (s << 1) + a - '0');//перед вычислением значения сообщения в int, для добавления в слолварь соответствия вектора ошибки(в инте) и синдрома, убираем остаток в 24 нулевых бита от вектора ошибки, до этого добавленный для расчета crc

                    syndromesDict.Add(CalcErrSyndromeCRC(bitsList), sequenceValue);//словарь хранит значения векторов ошибок и их значение crc в int
                    bitsList[i] = '0';
                }
            }
            catch(Exception ex)
            {

            }
        }

        //вычисление всех векторов ошибок и их crc для 2 ошибок в 88 битном теле сообщения
        private void CalcPermutations2(int vectorLength)
        {
            StringBuilder bitsList = new StringBuilder();
            for (int m = 0; m < vectorLength; m++)
            {
                bitsList.Append('0'); // Initialize starting sequence with each bite equal to zero
            }
            StringBuilder ending = new StringBuilder(24);
            for (int m = 0; m < 24; m++)
            {
                ending.Append('0');
            }
            bitsList.Append(ending);//добавили нулями до 112 бит, чтобы далее можно было вычислить crc от конкретного вектора ошибкт

            for (int i = 0; i < 87; i++)
            {
               
                if (bitsList[i].Equals('1'))
                {
                    continue;
                }
                bitsList[i] = '1';
                for (int j = i + 1; j < 88; j++)
                {
                    if (bitsList[j].Equals('1'))
                    {
                        continue;
                    }
                    bitsList[j] = '1';



                    StringBuilder bitsListMsg = new StringBuilder(bitsList.Length);
                    bitsListMsg.Append(bitsList);
                    BigInteger sequenceValue = bitsListMsg.Remove(88, 24).ToString().Aggregate(BigInteger.Zero, (s, a) => (s << 1) + a - '0');//перед вычислением значения сообщения в int, для добавления в слолварь соответствия вектора ошибки(в инте) и синдрома, убираем остаток в 24 нулевых бита от вектора ошибки, до этого добавленный для расчета crc
                    syndromesDict.Add(CalcErrSyndromeCRC(bitsList), sequenceValue);//словарь хранит значения векторов ошибок и их значение crc в int

                    bitsList[j] = '0';
                }
                bitsList[i] = '0';
            }
        }

        //вычисление всех векторов ошибок и их crc для 3 ошибок в 88 битном теле сообщения
        private void CalcPermutations3(int vectorLength)
        {

            StringBuilder bitsList = new StringBuilder();
            for (int m = 0; m < vectorLength; m++)
            {
                bitsList.Append('0'); // Initialize starting sequence with each bite equal to zero
            }
            StringBuilder ending = new StringBuilder(24);
            for (int m = 0; m < 24; m++)
            {
                ending.Append('0');
            }
            bitsList.Append(ending);//добавили нулями до 112 бит, чтобы далее можно было вычислить crc от конкретного вектора ошибкт
            for (int i = 0; i < 86; i++)
            {
                if (bitsList[i].Equals('1'))
                {
                    continue;
                }
                bitsList[i] = '1';
                for (int j = i + 1; j < 87; j++)
                {
                    if (bitsList[j].Equals('1'))
                    {
                        continue;
                    }
                    bitsList[j] = '1';
                    for (int k = j + 1; k < 88; k++)
                    {
                        if (bitsList[k].Equals('1'))
                        {
                            continue;
                        }
                        bitsList[k] = '1';


                        StringBuilder bitsListMsg = new StringBuilder(bitsList.Length);
                        bitsListMsg.Append(bitsList);
                        BigInteger sequenceValue = bitsListMsg.Remove(88, 24).ToString().Aggregate(BigInteger.Zero, (s, a) => (s << 1) + a - '0');//перед вычислением значения сообщения в int, для добавления в слолварь соответствия вектора ошибки(в инте) и синдрома, убираем остаток в 24 нулевых бита от вектора ошибки, до этого добавленный для расчета crc
                        syndromesDict.Add(CalcErrSyndromeCRC(bitsList), sequenceValue);//словарь хранит значения векторов ошибок и их значение crc в int
                        bitsList[k] = '0';
                    }
                    bitsList[j] = '0';
                }
                bitsList[i] = '0';
            }
        }

        //вычисление всех векторов ошибок и их crc для 4 ошибок в 88 битном теле сообщения
        private void CalcPermutations4(int vectorLength)
        {

            StringBuilder bitsList = new StringBuilder();
            for (int m = 0; m < vectorLength; m++)
            {
                bitsList.Append('0'); // Initialize starting sequence with each bite equal to zero
            }
            StringBuilder ending = new StringBuilder(24);
            for (int m = 0; m < 24; m++)
            {
                ending.Append('0');
            }
            bitsList.Append(ending);//добавили нулями до 112 бит, чтобы далее можно было вычислить crc от конкретного вектора ошибкт
            for (int i = 0; i < 85; i++)
            {
               
                if (bitsList[i].Equals('1'))
                {
                    continue;
                }
                bitsList[i] = '1';
                for (int j = i + 1; j < 86; j++)
                {
                    if (bitsList[j].Equals('1'))
                    {
                        continue;
                    }
                    bitsList[j] = '1';
                    for (int k = j + 1; k < 87; k++)
                    {
                        if (bitsList[k].Equals('1'))
                        {
                            continue;
                        }
                        bitsList[k] = '1';
                        for (int l = k + 1; l < 88; l++)
                        {
                            if (bitsList[l].Equals('1'))
                            {
                                continue;
                            }
                            bitsList[l] = '1';

                            StringBuilder bitsListMsg = new StringBuilder(bitsList.Length);
                            bitsListMsg.Append(bitsList);
                            BigInteger sequenceValue = System.Numerics.BigInteger.Parse(bitsListMsg.Remove(88, 24).ToString());//перед вычислением значения сообщения в int, для добавления в слолварь соответствия вектора ошибки(в инте) и синдрома, убираем остаток в 24 нулевых бита от вектора ошибки, до этого добавленный для расчета crc//for example error error vector is 00000....0001111 = 15 в инте(далее в словаре будет пара: синдром в инте и 15);
                            syndromesDict.Add(CalcErrSyndromeCRC(bitsList), sequenceValue);//словарь хранит значения векторов ошибок и их значение crc в int
                            bitsList[l] = '0';
                        }
                        bitsList[k] = '0';
                    }
                    bitsList[j] = '0';
                }
                bitsList[i] = '0';
            }
        }
        //вычисление всех векторов ошибок и их crc для 5 ошибок в 88 битном теле сообщения
        private void CalcPermutations5(int vectorLength)
        {
            StringBuilder bitsList = new StringBuilder();
            for (int m = 0; m < vectorLength; m++)
            {
                bitsList.Append('0'); // Initialize starting sequence with each bite equal to zero
            }
            StringBuilder ending = new StringBuilder(24);
            for (int m = 0; m < 24; m++)
            {
                ending.Append('0');
            }
            bitsList.Append(ending);//добавили нулями до 112 бит, чтобы далее можно было вычислить crc от конкретного вектора ошибкт
           
         
            for (int i = 0; i < 84; i++)
            {
                
                if (bitsList[i].Equals('1'))
                {
                    continue;
                }
                bitsList[i] = '1';
                for (int j = i + 1; j < 85; j++)
                {
                    if (bitsList[j].Equals('1'))
                    {
                        continue;
                    }
                    bitsList[j] = '1';
                    for (int k = j + 1; k < 86; k++)
                    {
                        if (bitsList[k].Equals('1'))
                        {
                            continue;
                        }
                        bitsList[k] = '1';
                        for (int l = k + 1; l < 87; l++)
                        {
                            if (bitsList[l].Equals('1'))
                            {
                                continue;
                            }
                            bitsList[l] = '1';
                            for(int m = l+1; m<88;m++)
                            {
                                if (bitsList[m].Equals('1'))
                                {
                                    continue;
                                }
                                bitsList[m] = '1';

                                StringBuilder bitsListMsg = new StringBuilder(bitsList.Length);
                                bitsListMsg.Append(bitsList);
                                BigInteger sequenceValue = bitsListMsg.Remove(88, 24).ToString().Aggregate(BigInteger.Zero, (s, a) => (s << 1) + a - '0');//перед вычислением значения сообщения в int, для добавления в слолварь соответствия вектора ошибки(в инте) и синдрома, убираем остаток в 24 нулевых бита от вектора ошибки, до этого добавленный для расчета crc//for example error vector is 00000...0011111 = 31;
                                syndromesDict.Add(CalcErrSyndromeCRC(bitsList), sequenceValue);//словарь хранит значения векторов ошибок и их значение crc в int
                                bitsList[m] = '0';
                            }
                            bitsList[l] = '0';
                        }
                        bitsList[k] = '0';
                    }
                    bitsList[j] = '0';
                }
                bitsList[i] = '0';
            }
        }
        private BigInteger CalcErrSyndromeCRC(StringBuilder vector)
        {
            StringBuilder errorBitVector = new StringBuilder(vector.Length);
            errorBitVector.Append(vector);
            //calculating crc of 112 bits error vector
            for (int i = 0; i <= errorBitVector.Length-24-1; i++)
            {

                if (errorBitVector[i].Equals('1'))
                {
                    for (int j = 0; j < polynom.Length; j++)
                    {

                        if (errorBitVector[i + j].Equals(polynom[j])) { errorBitVector[i + j] = '0'; }
                        else errorBitVector[i + j] = '1';

                    }
                }
            }

            BigInteger crcInt = System.Numerics.BigInteger.Parse(errorBitVector.ToString());//ненулевой! остаток(последние 24 бита) от вычисления crc от вектора ошибки и будет симптомом данной ошибки
            return crcInt;
            
        }

        private bool FixPacket(string badPacketInHex, string packetErrSyndrome)
        {
            
            BigInteger packetErrSyndromeInt = System.Numerics.BigInteger.Parse(packetErrSyndrome.ToString());
            BigInteger badPacketInInt = BigInteger.Parse(badPacketInHex, NumberStyles.HexNumber);
            BigInteger fixingVectorInt = 0;
            bool isKnownError = syndromesDict.TryGetValue(packetErrSyndromeInt, out fixingVectorInt);
            BigInteger correctPacket = -1;
            if (isKnownError)
            {
               fixingVectorInt = fixingVectorInt << 24;//добавляем к корректирующему вектору 24 нулевых бита остатка, так как корректируемый пакет приходит тоже с остатком
               correctPacket = badPacketInInt ^ fixingVectorInt;//xor испорченного пакета с вектором ошибки, совпадающего по синдрому с данным испорченным пакетом, в итоге получаем исправленный пакет

            }
            else
            {
                return false;
            }
            //проверка crc и возвращение true если все ок, пакет исправлен
            bool isPacketFixed = CheckCRC(correctPacket.ToString("X"), (int)downlinkFormat);
            if(isPacketFixed)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}

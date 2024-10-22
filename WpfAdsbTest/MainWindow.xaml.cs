using ADSBClientLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace WpfAdsbTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<PlaneInfo> List { get; set; }
        
        ADSBClient adsbClient;

        public string[] serialPortNames { get; set; }

        public string serialPortName { get; set; }
        public string serialPortBaudRate { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            serialPortNames = SerialPort.GetPortNames();

            List = new ObservableCollection<PlaneInfo>();
            //this.dataGrid1.DataContext = this;
            this.DataContext = this;
            dataGrid1.Items.Clear();
            dataGrid1.ItemsSource = List;    
            

            
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Do some actions before closing the window
            MessageBoxResult result = MessageBox.Show("Are you sure you want to exit?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.No)
            {
                // Cancel the closing operation
                e.Cancel = true;
            }
            else
            { 
                Application.Current.Shutdown(); 
            }
        }

        private void AdsbClient_OnUpdateADSBData(object sender, MyEventArgsADSBData e)
        {
            textBlockConsole.Text += Environment.NewLine;
            textBlockConsole.Text += Environment.NewLine;

            Task task3 = Task.Run(() => {
                var matchingvalue = List.FirstOrDefault(ListElementToCheck => ListElementToCheck.Icao.Contains(e.planeData.Icao));
                if (matchingvalue != null)
                {                   
                    //matchingvalue.Icao = e.planeData.Icao;
                    if (!e.planeData.ModelName.Equals("")) 
                    {
                        textBlockConsole.Text += "ModelName: " + e.planeData.ModelName + Environment.NewLine;
                        matchingvalue.ModelName = e.planeData.ModelName;
                    }
                    matchingvalue.Time = e.planeData.PackageReceiveTime.ToString();
                    if (e.planeData.Lat != -1) 
                    {
                        textBlockConsole.Text += "Lat: "+e.planeData.Lat + Environment.NewLine;
                        matchingvalue.Latitude = e.planeData.Lat;
                    }
                    if (e.planeData.Long != -1)
                    {
                        textBlockConsole.Text += "Long: " + e.planeData.Long + Environment.NewLine;
                        matchingvalue.Longitude = e.planeData.Long;
                    }
                    if (e.planeData.Altitude != -1)
                    {
                        textBlockConsole.Text += "Alt: " + e.planeData.Altitude + Environment.NewLine;
                        matchingvalue.Altitude = e.planeData.Altitude;
                    }
                    if(e.planeData.TrackAngle!=-1)
                    {
                        textBlockConsole.Text += "Track angle: " + e.planeData.TrackAngle + Environment.NewLine;
                        matchingvalue.Direction = e.planeData.TrackAngle;
                    }
                    if(e.planeData.AirSpeed!=-1)
                    {
                        textBlockConsole.Text += "AirSpeed: " + e.planeData.AirSpeed + Environment.NewLine;
                        matchingvalue.AirVelocity = e.planeData.AirSpeed;
                    }
                    //matchingvalue.Groundvelocity = e.planeData.GroundSpeed; 
                    if(!e.planeData.Country.Equals(""))
                    {
                        textBlockConsole.Text += "Country: " + e.planeData.Country + Environment.NewLine;
                        matchingvalue.Country = e.planeData.Country;
                    }
                    if(!e.planeData.Flag.Equals(""))
                    {
                        matchingvalue.Flag = "pack://application:,,,/Images/" + e.planeData.Flag;
                    }                 
                    
                }
                else
                {
                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        List.Add(new PlaneInfo() { Icao = e.planeData.Icao, ModelName = e.planeData.ModelName, Time = e.planeData.PackageReceiveTime.ToString(), Latitude = e.planeData.Lat, Longitude = e.planeData.Long, Altitude = e.planeData.Altitude, AirVelocity = e.planeData.AirSpeed, Groundvelocity = e.planeData.GroundSpeed, Direction = e.planeData.TrackAngle, Country = e.planeData.Country, Flag = "pack://application:,,,/Images/" + e.planeData.Flag });
                    });
                    textBlockConsole.Text += "ModelName: " + e.planeData.ModelName + Environment.NewLine;
                    textBlockConsole.Text += "Lat: " + e.planeData.Lat + Environment.NewLine;
                    textBlockConsole.Text += "Long: " + e.planeData.Long + Environment.NewLine;
                    textBlockConsole.Text += "Alt: " + e.planeData.Altitude + Environment.NewLine;
                    textBlockConsole.Text += "Track angle: " + e.planeData.TrackAngle + Environment.NewLine;
                    textBlockConsole.Text += "AirSpeed: " + e.planeData.AirSpeed + Environment.NewLine;
                    textBlockConsole.Text += "Country: " + e.planeData.Country + Environment.NewLine;

                }
            });
            Dispatcher.Invoke(new Action(() => myEllipse1.Fill = new SolidColorBrush(Colors.Blue)));
            Thread.Sleep(350);
            Dispatcher.Invoke(new Action(() => myEllipse1.Fill = new SolidColorBrush(Colors.White)));
        }
        private void AdsbClient_OnConnectADSB(object sender, EventArgs e)
        {

            Dispatcher.Invoke(new Action(() => myEllipse.Fill = new SolidColorBrush(Colors.Green))); 
           
            //Console.WriteLine("Соединение установлено.");
        }
        private void AdsbClient_OnDisconnectADSB(object sender, EventArgs e)
        {
             
            Dispatcher.Invoke(new Action(() => myEllipse.Fill = new SolidColorBrush(Colors.Red)));
            //Console.WriteLine("Соединение разорвано!");
            //Console.ReadLine();
        }

        private void ShowHideDetails(object sender, RoutedEventArgs e)
        {
            for (var vis = sender as Visual; vis != null; vis = VisualTreeHelper.GetParent(vis) as Visual)
            if (vis is DataGridRow)
            {
                var row = (DataGridRow)vis;
                row.DetailsVisibility =
                row.DetailsVisibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
                break;
            }
        }

        private void btnConnectTCP_Click(object sender, RoutedEventArgs e)
        {

            adsbClient = new ADSBClient();//распаршенные данные никуда(в смысле на udp клиента) отсылаться не будут
                                                     //ADSBClient adsbClient = new ADSBClient(true, "127.0.0.1", 16002);
                                                     //ADSBClient adsbClient = new ADSBClient(true, "192.168.0.11", 16002);//распаршенные данные будут отправляться по udp на указанный в скобках адрес и порт 
            adsbClient.OnUpdateADSBData += AdsbClient_OnUpdateADSBData;
            adsbClient.OnConnect += AdsbClient_OnConnectADSB;
            adsbClient.OnDisconnect += AdsbClient_OnDisconnectADSB;

            Task task3 = Task.Run(() =>
            {
                adsbClient.Connect();               
            }
            );
           

        }

        private void btnConnectCOM_Click(object sender, RoutedEventArgs e)
        {

            btnConnectCOM.IsEnabled = false;
            adsbClient = new ADSBClient();//распаршенные данные никуда(в смысле на udp клиента) отсылаться не будут
                                          //ADSBClient adsbClient = new ADSBClient(true, "127.0.0.1", 16002);
                                          //ADSBClient adsbClient = new ADSBClient(true, "192.168.0.11", 16002);//распаршенные данные будут отправляться по udp на указанный в скобках адрес и порт 
                                          //ADSBClient adsbClient = new ADSBClient(true, "192.168.0.11", 16002);//распаршенные данные будут отправляться по udp на указанный в скобках адрес и порт 
            adsbClient.OnUpdateADSBData += AdsbClient_OnUpdateADSBData;
            adsbClient.OnConnect += AdsbClient_OnConnectADSB;
            adsbClient.OnDisconnect += AdsbClient_OnDisconnectADSB;
            Task task3 = Task.Run(() =>
            {
                adsbClient.Connect(serialPortName, Int32.Parse(serialPortBaudRate));
            }
            );

        }

        private void btnDisconnect_Click(object sender, RoutedEventArgs e)
        {
            if (adsbClient != null)
            {
                adsbClient.Disconnect();
                btnConnectCOM.IsEnabled = true;
            }
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            if (List.Count > 0)
            {
                List.Clear();
            }
        }

        private void ComboBoxPortName_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
          serialPortName = e.Source.ToString();
        }

        private void TextBoxSpeedRate_TextChanged(object sender, TextChangedEventArgs e)
        {
            
        }
    }
}

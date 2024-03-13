using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace WpfAdsbTest
{
    public class PlaneInfo: INotifyPropertyChanged
    {
        // The event that notifies the grid table of property changes
        public event PropertyChangedEventHandler PropertyChanged;

        // A method that raises the event with the property name
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    // An example property with a backing field
    private string icao;
    public string Icao
    {
        get { return icao; }
        set
        {
            if(icao!=null && icao.Equals(value))
            {
                return;
            }
            icao = value;
            // Raise the event when the value is set
            NotifyPropertyChanged("Icao");
        }
    }

        private double longitude;
        public double Longitude
        {
            get { return longitude; }
            set
            {
                if(longitude==value)
                {
                    return;
                }
                longitude = value;
                // Raise the event when the value is set
                NotifyPropertyChanged("Longitude");
            }
        }
        private double latitude;
        public double Latitude
        {
            get { return latitude; }
            set
            {
                if (latitude == value)
                {
                    return;
                }
                latitude = value;
                // Raise the event when the value is set
                NotifyPropertyChanged("Latitude");
            }
        }

        private double airvelocity;
        public double AirVelocity
        {
            get { return airvelocity; }
            set
            {
                if (airvelocity == value)
                {
                    return;
                }
                airvelocity = value;
                // Raise the event when the value is set
                NotifyPropertyChanged("AirVelocity");
            }
        }

        private double groundvelocity;
        public double Groundvelocity
        {
            get { return groundvelocity; }
            set
            {
                if (groundvelocity == value)
                {
                    return;
                }
                groundvelocity = value;
                // Raise the event when the value is set
                NotifyPropertyChanged("Groundvelocity");
            }
        }
        private double altitude;
        public double Altitude
        {
            get { return altitude; }
            set
            {
                if (altitude == value)
                {
                    return;
                }
                altitude = value;
                // Raise the event when the value is set
                NotifyPropertyChanged("Altitude");
            }
        }

        private double direction;
        public double Direction
        {
            get { return direction; }
            set
            {
                if (direction == value)
                {
                    return;
                }
                direction = value;
                // Raise the event when the value is set
                NotifyPropertyChanged("Direction");
            }
        }


        private string country;
        public string Country
        {
            get { return country; }
            set
            {
                if (country!=null && country.Equals(value))
                {
                    return;
                }
                country = value;
                // Raise the event when the value is set
                NotifyPropertyChanged("Country");
            }
        }

        private string flag;
        public string Flag
        {
            get { return flag; }
            set
            {
                if (flag!=null && flag.Equals(value))
                {
                    return;
                }
                flag = value;
                // Raise the event when the value is set
                NotifyPropertyChanged("Flag");
            }
        }
        private string modelname;
        public string ModelName
        {
            get { return modelname; }
            set
            {
                if (modelname!=null && modelname.Equals(value))
                {
                    return;
                }
                modelname = value;
                // Raise the event when the value is set
                NotifyPropertyChanged("ModelName");
            }
        }

        private string time;
        public string Time
        {
            get { return time; }
            set
            {
                if (time!=null && time.Equals(value))
                {
                    return;
                }
                time = value;
                // Raise the event when the value is set
                NotifyPropertyChanged("Time");
            }
        }
    }
}

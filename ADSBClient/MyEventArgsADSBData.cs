using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlaneInfoLib;

namespace ADSBClientLib
{
    public class MyEventArgsADSBData:EventArgs
    {
        public PlaneData planeData;
        public MyEventArgsADSBData(PlaneData planeData)
        {
            this.planeData = planeData;
        }
    }
}

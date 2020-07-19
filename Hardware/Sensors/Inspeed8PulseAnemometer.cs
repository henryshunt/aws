using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Text;

namespace AWS.Hardware.Sensors
{
    internal class Inspeed8PulseAnemometer
    {
        public ListValueStore<KeyValuePair<DateTime, int>> WindSpeedStore { get; }
            = new ListValueStore<KeyValuePair<DateTime, int>>();

        public void Initialise()
        {

        }

        public static double CountToWindSpeed(int count)
        {
            return count * 0.31;
        }
    }
}

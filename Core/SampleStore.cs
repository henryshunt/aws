using System;
using System.Collections.Generic;

namespace AWS.Core
{
    internal class SampleStore
    {
        public List<double> AirTemperature;
        public List<double> RelativeHumidity;
        public List<KeyValuePair<DateTime, int>> WindSpeed;
        public List<KeyValuePair<DateTime, int>> WindDirection;
        public List<double> StationPressure;
        public List<double> Rainfall;

        public SampleStore()
        {
            Clear();
        }

        public void Clear()
        {
            AirTemperature = new List<double>();
            RelativeHumidity = new List<double>();
            WindSpeed = new List<KeyValuePair<DateTime, int>>();
            WindDirection = new List<KeyValuePair<DateTime, int>>();
            StationPressure = new List<double>();
            Rainfall = new List<double>();
        }
    }
}

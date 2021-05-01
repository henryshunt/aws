using System;
using System.Collections.Generic;

namespace Aws.Core
{
    internal class SampleStore
    {
        public readonly List<double> AirTemperature = new List<double>();
        public readonly List<double> RelativeHumidity = new List<double>();
        public readonly List<KeyValuePair<DateTime, double>> WindSpeed
            = new List<KeyValuePair<DateTime, double>>();
        public readonly List<KeyValuePair<DateTime, int>> WindDirection
            = new List<KeyValuePair<DateTime, int>>();
        public readonly List<double> Rainfall = new List<double>();
        public readonly List<double> StationPressure = new List<double>();
    }
}

using System;
using System.Collections.Generic;

namespace Aws.Core
{
    /// <summary>
    /// Represents a structure for storing the samples from sensors.
    /// </summary>
    internal class SampleStore
    {
        public readonly List<double> AirTemperature = new List<double>();

        public readonly List<double> RelativeHumidity = new List<double>();

        public readonly List<KeyValuePair<DateTime, double>> WindSpeed
            = new List<KeyValuePair<DateTime, double>>();

        public readonly List<KeyValuePair<DateTime, double>> WindDirection
            = new List<KeyValuePair<DateTime, double>>();

        public readonly List<double> Rainfall = new List<double>();

        public readonly List<bool> SunshineDuration = new List<bool>();

        public readonly List<double> StationPressure = new List<double>();
    }
}

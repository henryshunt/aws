using System;
using System.Collections.Generic;

namespace Aws.Misc
{
    /// <summary>
    /// Represents a structure for storing samples taken from each of the sensors.
    /// </summary>
    internal class SampleBuffer
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

        /// <summary>
        /// Initialises a new instance of the <see cref="SampleBuffer"/> class.
        /// </summary>
        public SampleBuffer() { }
    }
}

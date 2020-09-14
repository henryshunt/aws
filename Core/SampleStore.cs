using System;
using System.Collections.Generic;

namespace AWS.Core
{
    internal class SampleStore
    {
        public readonly List<double> AirTemperature = new List<double>();
        public readonly List<double> RelativeHumidity = new List<double>();
        public readonly List<KeyValuePair<DateTime, int>> WindSpeed = new List<KeyValuePair<DateTime, int>>();
        public readonly List<KeyValuePair<DateTime, int>> WindDirection = new List<KeyValuePair<DateTime, int>>();
        public readonly List<double> Rainfall = new List<double>();
        public readonly List<double> BarometricPressure = new List<double>();
        public readonly List<int> SunshineDuration = new List<int>();
        public readonly List<double> SoilTemperature10 = new List<double>();
        public readonly List<double> SoilTemperature30 = new List<double>();
        public readonly List<double> SoilTemperature100 = new List<double>();

        public void Clear()
        {
            AirTemperature.Clear();
            RelativeHumidity.Clear();
            WindSpeed.Clear();
            WindDirection.Clear();
            Rainfall.Clear();
            BarometricPressure.Clear();
            SunshineDuration.Clear();
            SoilTemperature10.Clear();
            SoilTemperature30.Clear();
            SoilTemperature100.Clear();
        }
    }
}

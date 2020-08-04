using System;
using System.Collections.Generic;
using System.Text;

namespace AWS.Routines
{
    internal class Report
    {
        public DateTime Time { get; set; }

        public double? AirTemperature { get; set; } = null;
        public double? RelativeHumidity { get; set; } = null;
        public double? DewPoint { get; set; } = null;
        public double? WindSpeed { get; set; } = null;
        public double? WindDirection { get; set; } = null;
        public double? WindGustSpeed { get; set; } = null;
        public double? WindGustDirection { get; set; } = null;
        public double? Rainfall { get; set; } = null;
        public double? StationPressure { get; set; } = null;
        public double? MSLPressure { get; set; } = null;
        public double? SoilTemperature10 { get; set; } = null;
        public double? SoilTemperature30 { get; set; } = null;
        public double? SoilTemperature100 { get; set; } = null;

        public Report(DateTime time)
        {
            Time = time;
        }
    }
}

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
        public double? WindGust { get; set; } = null;
        public double? Rainfall { get; set; } = null;
        public double? BarometricPressure { get; set; } = null;
        public double? MslPressure { get; set; } = null;
        public int? SunshineDuration { get; set; } = null;
        public double? SoilTemperature10 { get; set; } = null;
        public double? SoilTemperature30 { get; set; } = null;
        public double? SoilTemperature100 { get; set; } = null;

        public Report(DateTime time)
        {
            Time = time;
        }

        public override string ToString()
        {
            return string.Format(
                "T:{0:0.0}, H:{1:0.0}, WS:{3:0.0}, WD:{4:0}, WG:{5:0.0}, R:{6:0.000}, P:{2:0.0}",
                AirTemperature, RelativeHumidity, BarometricPressure, WindSpeed, WindDirection % 360,
                WindGust, Rainfall);
        }
    }
}

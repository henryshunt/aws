using System;
using System.Collections.Generic;
using System.Text;

namespace Aws.Routines
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

        public Report(DateTime time)
        {
            Time = time;
        }

        public override string ToString()
        {
            return string.Format(
                "{0} -- T:{1:0.0}, H:{2:0.0}, DP:{3:0.0}, WS:{4:0.0}, WG:{5:0.0}, WD:{6:0}, R:{7:0.000}, P:{8:0.0}",
                Time, AirTemperature, RelativeHumidity, DewPoint, WindSpeed, WindGust, WindDirection,
                Rainfall, BarometricPressure);
        }
    }
}

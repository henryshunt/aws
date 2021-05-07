using System;

namespace Aws.Misc
{
    public class Report
    {
        public DateTime Time { get; set; }
        public double? AirTemperature { get; set; } = null;
        public double? RelativeHumidity { get; set; } = null;
        public double? DewPoint { get; set; } = null;
        public double? WindSpeed { get; set; } = null;
        public int? WindDirection { get; set; } = null;
        public double? WindGust { get; set; } = null;
        public double? Rainfall { get; set; } = null;
        public int? SunshineDuration { get; set; } = null;
        public double? StationPressure { get; set; } = null;
        public double? MslPressure { get; set; } = null;

        public Report(DateTime time)
        {
            Time = time;
        }
    }
}

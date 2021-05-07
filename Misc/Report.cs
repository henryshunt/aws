using System;

namespace Aws.Misc
{
    public class Report
    {
        /// <summary>
        /// The time of the report.
        /// </summary>
        public DateTime Time { get; set; }

        /// <summary>
        /// Air temperature in degrees celsius.
        /// </summary>
        public double? AirTemperature { get; set; } = null;

        /// <summary>
        /// Relative humidity in percent.
        /// </summary>
        public double? RelativeHumidity { get; set; } = null;

        /// <summary>
        /// Dew point in degrees celsius.
        /// </summary>
        public double? DewPoint { get; set; } = null;

        /// <summary>
        /// Wind speed in metres per second.
        /// </summary>
        public double? WindSpeed { get; set; } = null;

        /// <summary>
        /// Wind direction in degrees.
        /// </summary>
        public int? WindDirection { get; set; } = null;

        /// <summary>
        /// Wind gust in metres per second.
        /// </summary>
        public double? WindGust { get; set; } = null;

        /// <summary>
        /// Rainfall in millimetres.
        /// </summary>
        public double? Rainfall { get; set; } = null;

        /// <summary>
        /// Sunshine duration in seconds.
        /// </summary>
        public int? SunshineDuration { get; set; } = null;

        /// <summary>
        /// Station pressure in hectopascals.
        /// </summary>
        public double? StationPressure { get; set; } = null;

        /// <summary>
        /// Mean sea level pressure in hectopascals.
        /// </summary>
        public double? MslPressure { get; set; } = null;

        /// <summary>
        /// Initialises a new instance of the <see cref="Report"/> class.
        /// </summary>
        /// <param name="time">
        /// The time of the report.
        /// </param>
        public Report(DateTime time)
        {
            Time = time;
        }
    }
}

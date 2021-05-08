using System;

namespace Aws.Misc
{
    /// <summary>
    /// Represents an observation of the weather conditions at a certain time. Note that this is different from a
    /// sample, which is a single instantaneous measurement from a sensor. An observation summarises many samples over 
    /// a short period, primarily one minute.
    /// </summary>
    internal class Observation
    {
        /// <summary>
        /// The time of the observation, in UTC.
        /// </summary>
        public DateTime Time { get; set; }

        /// <summary>
        /// The average air temperature for the minute leading up to the time, in degrees celsius.
        /// </summary>
        public double? AirTemperature { get; set; } = null;

        /// <summary>
        /// The average relative humidity for the minute leading up to the time, in percent.
        /// </summary>
        public double? RelativeHumidity { get; set; } = null;

        /// <summary>
        /// The derived dew point in degrees celsius.
        /// </summary>
        public double? DewPoint { get; set; } = null;

        /// <summary>
        /// The average wind speed for the ten minutes leading up to the time, in metres per second.
        /// </summary>
        public double? WindSpeed { get; set; } = null;

        /// <summary>
        /// The average wind direction for the ten minutes leading up to the time, in degrees.
        /// </summary>
        public int? WindDirection { get; set; } = null;

        /// <summary>
        /// The maximum three-second wind gust in the ten minutes leading up to the time, in metres per second.
        /// </summary>
        public double? WindGust { get; set; } = null;

        /// <summary>
        /// The total rainfall in the minute leading up to the time, in millimetres.
        /// </summary>
        public double? Rainfall { get; set; } = null;

        /// <summary>
        /// The total tunshine duration in the minute leading up to the time, in seconds.
        /// </summary>
        public int? SunshineDuration { get; set; } = null;

        /// <summary>
        /// The average pressure at station elevation for the minute leading up to the time, in hectopascals.
        /// </summary>
        public double? StationPressure { get; set; } = null;

        /// <summary>
        /// The derived mean sea level pressure in hectopascals.
        /// </summary>
        public double? MslPressure { get; set; } = null;

        /// <summary>
        /// Initialises a new instance of the <see cref="Observation"/> class.
        /// </summary>
        /// <param name="time">
        /// The time of the observation, in UTC.
        /// </param>
        public Observation(DateTime time)
        {
            Time = time;
        }
    }
}

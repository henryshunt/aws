using System;

namespace Aws.Misc
{
    /// <summary>
    /// Represents statistics for the observations logged over a day in the local time zone.
    /// </summary>
    internal class DailyStatistics
    {
        /// <summary>
        /// The date, in the local time zone, that the statistics cover.
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// The average air temperature for the day, in degrees celsius.
        /// </summary>
        public double? AirTemperatureAverage { get; set; } = null;

        /// <summary>
        /// The minimum air temperature for the day, in degrees celsius.
        /// </summary>
        public double? AirTemperatureMinimum { get; set; } = null;

        /// <summary>
        /// The maximum air temperature for the day, in degrees celsius.
        /// </summary>
        public double? AirTemperatureMaximum { get; set; } = null;

        /// <summary>
        /// The average relative humidity for the day, in percent.
        /// </summary>
        public double? RelativeHumidityAverage { get; set; } = null;

        /// <summary>
        /// The minimum relative humidity for the day, in percent.
        /// </summary>
        public double? RelativeHumidityMinimum { get; set; } = null;

        /// <summary>
        /// The maximum relative humidity for the day, in percent.
        /// </summary>
        public double? RelativeHumidityMaximum { get; set; } = null;

        /// <summary>
        /// The average dew point for the day, in degrees celsius.
        /// </summary>
        public double? DewPointAverage { get; set; } = null;

        /// <summary>
        /// The minimum dew point for the day, in degrees celsius.
        /// </summary>
        public double? DewPointMinimum { get; set; } = null;

        /// <summary>
        /// The maximum dew point for the day, in degrees celsius.
        /// </summary>
        public double? DewPointMaximum { get; set; } = null;

        /// <summary>
        /// The average ten-minute wind speed for the day, in metres per second.
        /// </summary>
        public double? WindSpeedAverage { get; set; } = null;

        /// <summary>
        /// The minimum ten-minute wind speed for the day, in metres per second.
        /// </summary>
        public double? WindSpeedMinimum { get; set; } = null;

        /// <summary>
        /// The maximum ten-minute wind speed for the day, in metres per second.
        /// </summary>
        public double? WindSpeedMaximum { get; set; } = null;

        /// <summary>
        /// The average ten-minute wind direction for the day, in degrees.
        /// </summary>
        public int? WindDirectionAverage { get; set; } = null;

        /// <summary>
        /// The average ten-minute wind gust for the day, in metres per second.
        /// </summary>
        public double? WindGustAverage { get; set; } = null;

        /// <summary>
        /// The minimum ten-minute wind gust for the day, in metres per second.
        /// </summary>
        public double? WindGustMinimum { get; set; } = null;

        /// <summary>
        /// The maximum ten-minute wind gust for the day, in metres per second.
        /// </summary>
        public double? WindGustMaximum { get; set; } = null;

        /// <summary>
        /// The total rainfall for the day, in millimetres.
        /// </summary>
        public double? RainfallTotal { get; set; } = null;

        /// <summary>
        /// The total sunshine duration for the day, in seconds.
        /// </summary>
        public int? SunshineDurationTotal { get; set; } = null;

        /// <summary>
        /// The average mean sea level pressure for the day, in hectopascals.
        /// </summary>
        public double? MslPressureAverage { get; set; } = null;

        /// <summary>
        /// The minimum mean sea level pressure for the day, in hectopascals.
        /// </summary>
        public double? MslPressureMinimum { get; set; } = null;

        /// <summary>
        /// The maximum mean sea level pressure for the day, in hectopascals.
        /// </summary>
        public double? MslPressureMaximum { get; set; } = null;

        /// <summary>
        /// Initialises a new instance of the <see cref="DailyStatistics"/> class.
        /// </summary>
        /// <param name="date">
        /// The date, in the local time zone, that the statistics cover.
        /// </param>
        public DailyStatistics(DateTime date)
        {
            Date = date;
        }
    }
}

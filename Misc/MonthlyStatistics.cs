using System;

namespace Aws.Misc
{
    /// <summary>
    /// Represents statistics for the observations logged over a month.
    /// </summary>
    internal class MonthlyStatistics
    {
        /// <summary>
        /// The year that the month is part of.
        /// </summary>
        private int year;

        /// <summary>
        /// The year that the month is part of.
        /// </summary>
        public int Year
        {
            get { return year; }
            set
            {
                if (value < 0)
                    throw new ArgumentException("value was less than zero", nameof(value));
                else year = value;
            }
        }

        /// <summary>
        /// The month (1 through 12) that the statistics cover.
        /// </summary>
        private int month;

        /// <summary>
        /// The month (1 through 12) that the statistics cover.
        /// </summary>
        public int Month
        {
            get { return month; }
            set
            {
                if (month < 1 || month > 12)
                    throw new ArgumentException("value was not in the range 1 through 12", "value");
                else month = value;
            }
        }

        /// <summary>
        /// The average air temperature for the month, in degrees celsius.
        /// </summary>
        public double? AirTemperatureAverage { get; set; } = null;

        /// <summary>
        /// The minimum air temperature for the month, in degrees celsius.
        /// </summary>
        public double? AirTemperatureMinimum { get; set; } = null;

        /// <summary>
        /// The maximum air temperature for the month, in degrees celsius.
        /// </summary>
        public double? AirTemperatureMaximum { get; set; } = null;

        /// <summary>
        /// The average relative humidity for the month, in percent.
        /// </summary>
        public double? RelativeHumidityAverage { get; set; } = null;

        /// <summary>
        /// The minimum relative humidity for the month, in percent.
        /// </summary>
        public double? RelativeHumidityMinimum { get; set; } = null;

        /// <summary>
        /// The maximum relative humidity for the month, in percent.
        /// </summary>
        public double? RelativeHumidityMaximum { get; set; } = null;

        /// <summary>
        /// The average dew point for the month, in degrees celsius.
        /// </summary>
        public double? DewPointAverage { get; set; } = null;

        /// <summary>
        /// The minimum dew point for the month, in degrees celsius.
        /// </summary>
        public double? DewPointMinimum { get; set; } = null;

        /// <summary>
        /// The maximum dew point for the month, in degrees celsius.
        /// </summary>
        public double? DewPointMaximum { get; set; } = null;

        /// <summary>
        /// The average ten-minute wind speed for the month, in metres per second.
        /// </summary>
        public double? WindSpeedAverage { get; set; } = null;

        /// <summary>
        /// The minimum ten-minute wind speed for the month, in metres per second.
        /// </summary>
        public double? WindSpeedMinimum { get; set; } = null;

        /// <summary>
        /// The maximum ten-minute wind speed for the month, in metres per second.
        /// </summary>
        public double? WindSpeedMaximum { get; set; } = null;

        /// <summary>
        /// The average ten-minute wind direction for the month, in degrees.
        /// </summary>
        public int? WindDirectionAverage { get; set; } = null;

        /// <summary>
        /// The average ten-minute wind gust for the month, in metres per second.
        /// </summary>
        public double? WindGustAverage { get; set; } = null;

        /// <summary>
        /// The minimum ten-minute wind gust for the month, in metres per second.
        /// </summary>
        public double? WindGustMinimum { get; set; } = null;

        /// <summary>
        /// The maximum ten-minute wind gust for the month, in metres per second.
        /// </summary>
        public double? WindGustMaximum { get; set; } = null;

        /// <summary>
        /// The total rainfall for the month, in millimetres.
        /// </summary>
        public double? RainfallTotal { get; set; } = null;

        /// <summary>
        /// The total sunshine duration for the month, in seconds.
        /// </summary>
        public int? SunshineDurationTotal { get; set; } = null;

        /// <summary>
        /// The average mean sea level pressure for the month, in hectopascals.
        /// </summary>
        public double? MslPressureAverage { get; set; } = null;

        /// <summary>
        /// The minimum mean sea level pressure for the month, in hectopascals.
        /// </summary>
        public double? MslPressureMinimum { get; set; } = null;

        /// <summary>
        /// The maximum mean sea level pressure for the month, in hectopascals.
        /// </summary>
        public double? MslPressureMaximum { get; set; } = null;

        /// <summary>
        /// Initialises a new instance of the <see cref="MonthlyStatistics"/> class.
        /// </summary>
        /// <param name="year">
        /// The year that the month is part of.
        /// </param>
        /// <param name="month">
        /// The month (1 through 12) that the statistics cover.
        /// </param>
        public MonthlyStatistics(int year, int month)
        {
            if (year < 0)
                throw new ArgumentException(nameof(year) + " was less than zero", nameof(year));
            else this.year = year;

            if (month < 1 || month > 12)
            {
                throw new ArgumentException(
                    nameof(month) + " was not in the range 1 through 12", nameof(month));
            }
            else this.month = month;
        }
    }
}

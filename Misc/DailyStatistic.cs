using System;

namespace Aws.Misc
{
    public class DailyStatistic
    {
        public DateTime Date { get; private set; }
        public double? AirTemperatureAverage { get; set; } = null;
        public double? AirTemperatureMinimum { get; set; } = null;
        public double? AirTemperatureMaximum { get; set; } = null;
        public double? RelativeHumidityAverage { get; set; } = null;
        public double? RelativeHumidityMinimum { get; set; } = null;
        public double? RelativeHumidityMaximum { get; set; } = null;
        public double? WindSpeedAverage { get; set; } = null;
        public double? WindSpeedMinimum { get; set; } = null;
        public double? WindSpeedMaximum { get; set; } = null;
        public int? WindDirectionAverage { get; set; } = null;
        public double? WindGustAverage { get; set; } = null;
        public double? WindGustMinimum { get; set; } = null;
        public double? WindGustMaximum { get; set; } = null;
        public double? RainfallTotal { get; set; } = null;
        public int? SunshineDurationTotal { get; set; } = null;
        public double? MslPressureAverage { get; set; } = null;
        public double? MslPressureMinimum { get; set; } = null;
        public double? MslPressureMaximum { get; set; } = null;

        public DailyStatistic(DateTime date)
        {
            Date = date;
        }
    }
}

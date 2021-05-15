namespace Aws.Sensors
{
    /// <summary>
    /// Represents configuration data for a <see cref="Satellite"/> device.
    /// </summary>
    internal class SatelliteConfiguration
    {
        /// <summary>
        /// Indicates whether the wind speed sensor is enabled.
        /// </summary>
        public bool WindSpeedEnabled { get; set; } = false;

        /// <summary>
        /// The pin that the wind speed sensor is connected to.
        /// </summary>
        public int WindSpeedPin { get; set; }

        /// <summary>
        /// Indicates whether the wind direction sensor is enabled.
        /// </summary>
        public bool WindDirectionEnabled { get; set; } = false;

        /// <summary>
        /// The pin that the wind direction sensor is connected to.
        /// </summary>
        public int WindDirectionPin { get; set; }

        /// <summary>
        /// Indicates whether the sunshine duration pin is enabled.
        /// </summary>
        public bool SunshineDurationEnabled { get; set; } = false;

        /// <summary>
        /// The pin that the sunshine duration sensor is connected to.
        /// </summary>
        public int SunshineDurationPin { get; set; }

        /// <summary>
        /// Initialises a new instance of the <see cref="SatelliteConfiguration"/> class.
        /// </summary>
        public SatelliteConfiguration() { }

        /// <summary>
        /// Produces a JSON string containing the configuration data.
        /// </summary>
        /// <returns>
        /// The produced JSON string.
        /// </returns>
        public string ToJsonString()
        {
            string result = "{";

            result += "\"windSpeed\":" + WindSpeedEnabled.ToString().ToLower();
            if (WindSpeedEnabled)
                result += ",\"windSpeedPin\":" + WindSpeedPin.ToString().ToLower();

            result += ",\"windDir\":" + WindDirectionEnabled.ToString().ToLower();
            if (WindDirectionEnabled)
                result += ",\"windDirPin\":" + WindDirectionPin.ToString().ToLower();

            result += ",\"sunDur\":" + SunshineDurationEnabled.ToString().ToLower();
            if (SunshineDurationEnabled)
                result += ",\"sunDurPin\":" + SunshineDurationPin.ToString().ToLower();

            return result + "}";
        }
    }
}

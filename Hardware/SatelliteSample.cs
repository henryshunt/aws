using Newtonsoft.Json;

namespace Aws.Hardware
{
    /// <summary>
    /// Represents a sample from a <see cref="Satellite"/> device.
    /// </summary>
    internal class SatelliteSample
    {
        /// <summary>
        /// The number of eighths of a rotation the wind speed sensor has made since the last sample or, if this is the
        /// first sample, since the device was configured.
        /// </summary>
        [JsonProperty("windSpeed")]
        public int? WindSpeed { get; set; } = null;

        /// <summary>
        /// The wind direction in degrees.
        /// </summary>
        [JsonProperty("windDir")]
        public double? WindDirection { get; set; } = null;

        /// <summary>
        /// Indicates whether it is sunny or not.
        /// </summary>
        [JsonProperty("sunDur")]
        public bool? SunshineDuration { get; set; } = null;
    }
}

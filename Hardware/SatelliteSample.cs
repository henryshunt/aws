using Newtonsoft.Json;

namespace Aws.Hardware
{
    internal class SatelliteSample
    {
        [JsonProperty("windSpeed")]
        public int? WindSpeed { get; set; } = null;

        [JsonProperty("windDir")]
        public double? WindDirection { get; set; } = null;

        [JsonProperty("sunDur")]
        public bool? SunshineDuration { get; set; } = null;
    }
}

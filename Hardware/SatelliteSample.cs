using Newtonsoft.Json;

namespace AWS.Hardware
{
    internal class SatelliteSample
    {
        [JsonProperty("windSpeed")]
        public int? WindSpeed { get; set; } = null;

        [JsonProperty("windDirection")]
        public int? WindDirection { get; set; } = null;
    }
}

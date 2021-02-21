using Newtonsoft.Json;

namespace Aws.Hardware
{
    internal class SatelliteSample
    {
        [JsonProperty("windSpeed")]
        public int? WindSpeed { get; set; } = null;

        [JsonProperty("windDirection")]
        public double? WindDirection { get; set; } = null;
    }
}

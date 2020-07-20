using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace AWS.Hardware
{
    internal class SatelliteSample
    {
        [JsonProperty("windSpeed")]
        public double? WindSpeed { get; set; } = null;

        [JsonProperty("windDirection")]
        public double? WindDirection { get; set; } = null;
    }
}

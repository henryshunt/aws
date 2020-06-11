using AWS.Hardware.Sensors;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;

namespace AWS.Routines
{
    internal class Configuration
    {
        public static Configuration Load(string filePath)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.MissingMemberHandling = MissingMemberHandling.Error;

            string json = File.ReadAllText(filePath);
            Configuration config = JsonConvert.DeserializeObject<Configuration>(json, settings);

            // Create a list of satellite IDs from those referenced by the sensors
            if (config.Sensors.WindSpeed.SatelliteID != null)
                config.Sensors.SatelliteIDs.Add((int)config.Sensors.WindSpeed.SatelliteID);
            if (config.Sensors.WindDirection.SatelliteID != null)
                config.Sensors.SatelliteIDs.Add((int)config.Sensors.WindDirection.SatelliteID);

            return config;
        }

        public static bool Validate(Configuration configuration)
        {
            return true;
        }


        [JsonProperty("schedulingClockPin")]
        public int SchedulingClockPin { get; set; }

        [JsonProperty("logger")]
        public LoggerJSON Logger { get; set; }

        internal class LoggerJSON
        {

        }


        [JsonProperty("transmitter")]
        public TransmitterJSON Transmitter { get; set; }

        internal class TransmitterJSON
        {
            [JsonProperty("transmitReports")]
            public bool TransmitReports { get; set; }
        }


        [JsonProperty("sensors")]
        public SensorsJSON Sensors { get; set; }

        public class SensorsJSON
        {
            [JsonIgnore]
            public HashSet<int> SatelliteIDs { get; set; } = new HashSet<int>();

            [JsonProperty("airTemperature")]
            public WindSpeedJSON AirTemperature { get; set; }

            internal class AirTemperatureJSON
            {
                [JsonProperty("enabled")]
                public bool Enabled { get; set; }
                [JsonProperty("satellite")]
                public int? SatelliteID { get; set; }
            }

            [JsonProperty("windSpeed")]
            public WindSpeedJSON WindSpeed { get; set; }

            internal class WindSpeedJSON
            {
                [JsonProperty("enabled")]
                public bool Enabled { get; set; }
                [JsonProperty("pin")]
                public int? Pin { get; set; }
                [JsonProperty("satellite")]
                public int? SatelliteID { get; set; }
            }

            [JsonProperty("windDirection")]
            public WindSpeedJSON WindDirection { get; set; }

            internal class WindDirectionJSON
            {
                [JsonProperty("enabled")]
                public bool Enabled { get; set; }
                [JsonProperty("pin")]
                public int? Pin { get; set; }
                [JsonProperty("satellite")]
                public int? SatelliteID { get; set; }
            }

            [JsonProperty("rainfall")]
            public RainfallJSON Rainfall { get; set; }

            internal class RainfallJSON
            {
                [JsonProperty("enabled")]
                public bool Enabled { get; set; }
                [JsonProperty("pin")]
                public int? Pin { get; set; }
                [JsonProperty("satellite")]
                public int? SatelliteID { get; set; }
            }
        }
    }
}
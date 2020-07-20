using Newtonsoft.Json;
using System.IO;

namespace AWS.Core
{
    internal class Configuration
    {
        public static Configuration Load(string filePath)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.MissingMemberHandling = MissingMemberHandling.Error;

            string json = File.ReadAllText(filePath);
            Configuration config = JsonConvert.DeserializeObject<Configuration>(json, settings);

            //// Create a list of satellite IDs from those referenced by the sensors
            //if (config.Sensors.WindSpeed.SatelliteID != null)
            //    config.Sensors.SatelliteIDs.Add((int)config.Sensors.WindSpeed.SatelliteID);
            //if (config.Sensors.WindDirection.SatelliteID != null)
            //    config.Sensors.SatelliteIDs.Add((int)config.Sensors.WindDirection.SatelliteID);

            return config;
        }

        public static bool Validate(Configuration configuration)
        {
            return true;
        }


        [JsonProperty("dataLEDPin")]
        public int DataLedPin { get; set; }
        [JsonProperty("errorLEDPin")]
        public int ErrorLedPin { get; set; }
        [JsonProperty("powerLEDPin")]
        public int PowerLedPin { get; set; }

        [JsonProperty("clockTickPin")]
        public int ClockTickPin { get; set; }


        [JsonProperty("logger")]
        public LoggerJson Logger { get; set; }

        internal class LoggerJson
        {

        }


        [JsonProperty("transmitter")]
        public TransmitterJson Transmitter { get; set; }

        internal class TransmitterJson
        {
            [JsonProperty("transmitReports")]
            public bool TransmitReports { get; set; }
        }


        [JsonProperty("sensors")]
        public SensorsJson Sensors { get; set; }

        public class SensorsJson
        {
            [JsonProperty("airTemperature")]
            public AirTemperatureJson AirTemperature { get; set; }

            internal class AirTemperatureJson
            {
                [JsonProperty("enabled")]
                public bool Enabled { get; set; }
            }

            [JsonProperty("satellite1")]
            public Satellite1Json Satellite1 { get; set; }

            public class Satellite1Json
            {
                [JsonProperty("windSpeed")]
                public WindSpeedJson WindSpeed { get; set; }

                internal class WindSpeedJson
                {
                    [JsonProperty("enabled")]
                    public bool Enabled { get; set; }
                    [JsonProperty("pin")]
                    public int? Pin { get; set; }
                }

                [JsonProperty("windDirection")]
                public WindDirectionJson WindDirection { get; set; }

                internal class WindDirectionJson
                {
                    [JsonProperty("enabled")]
                    public bool Enabled { get; set; }
                    [JsonProperty("pin")]
                    public int? Pin { get; set; }
                }
            }

            [JsonProperty("rainfall")]
            public RainfallJson Rainfall { get; set; }

            internal class RainfallJson
            {
                [JsonProperty("enabled")]
                public bool Enabled { get; set; }
                [JsonProperty("pin")]
                public int? Pin { get; set; }
            }
        }
    }
}
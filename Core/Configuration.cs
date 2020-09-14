using Newtonsoft.Json;
using System;
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

            if (config.Sensors.Satellite1.WindSpeed.Enabled || config.Sensors.Satellite1.WindDirection.Enabled)
                config.Sensors.Satellite1.Enabled = true;

            return config;
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
        public ConfigLogger Logger { get; set; }

        internal class ConfigLogger
        {

        }


        [JsonProperty("transmitter")]
        public ConfigTransmitter Transmitter { get; set; }

        internal class ConfigTransmitter
        {
            [JsonProperty("transmitReports")]
            public bool TransmitReports { get; set; }
        }


        [JsonProperty("sensors")]
        public ConfigSensors Sensors { get; set; }

        public class ConfigSensors
        {
            [JsonProperty("airTemperature")]
            public ConfigAirTemperature AirTemperature { get; set; }

            [JsonProperty("relativeHumidity")]
            public ConfigRelativeHumidity RelativeHumidity { get; set; }

            [JsonProperty("satellite1")]
            public ConfigSatellite1 Satellite1 { get; set; }

            public class ConfigSatellite1
            {
                [JsonIgnore]
                public bool Enabled { get; set; }

                [JsonProperty("windSpeed")]
                public ConfigWindSpeed WindSpeed { get; set; }

                [JsonProperty("windDirection")]
                public ConfigWindDirection WindDirection { get; set; }
            }

            [JsonProperty("rainfall")]
            public ConfigRainfall Rainfall { get; set; }

            [JsonProperty("barometricPressure")]
            public ConfigBarometricPressure BarometricPressure { get; set; }

            [JsonProperty("sunshineDuration")]
            public ConfigSunshineDuration SunshineDuration { get; set; }

            [JsonProperty("soilTemperature10")]
            public ConfigSoilTemperature10 SoilTemperature10 { get; set; }

            [JsonProperty("soilTemperature30")]
            public ConfigSoilTemperature30 SoilTemperature30 { get; set; }

            [JsonProperty("soilTemperature100")]
            public ConfigSoilTemperature100 SoilTemperature100 { get; set; }


            internal class ConfigAirTemperature
            {
                [JsonProperty("enabled")]
                public bool Enabled { get; set; }

                [JsonProperty("pin")]
                public int? Pin { get; set; }
            }

            internal class ConfigRelativeHumidity
            {
                [JsonProperty("enabled")]
                public bool Enabled { get; set; }
            }

            internal class ConfigWindSpeed
            {
                [JsonProperty("enabled")]
                public bool Enabled { get; set; }

                [JsonProperty("pin")]
                public int? Pin { get; set; }
            }

            internal class ConfigWindDirection
            {
                [JsonProperty("enabled")]
                public bool Enabled { get; set; }

                [JsonProperty("pin")]
                public int? Pin { get; set; }
            }

            internal class ConfigRainfall
            {
                [JsonProperty("enabled")]
                public bool Enabled { get; set; }

                [JsonProperty("pin")]
                public int? Pin { get; set; }
            }

            internal class ConfigBarometricPressure
            {
                [JsonProperty("enabled")]
                public bool Enabled { get; set; }
            }

            internal class ConfigSunshineDuration
            {
                [JsonProperty("enabled")]
                public bool Enabled { get; set; }

                [JsonProperty("pin")]
                public int? Pin { get; set; }
            }

            internal class ConfigSoilTemperature10
            {
                [JsonProperty("enabled")]
                public bool Enabled { get; set; }

                [JsonProperty("pin")]
                public int? Pin { get; set; }
            }

            internal class ConfigSoilTemperature30
            {
                [JsonProperty("enabled")]
                public bool Enabled { get; set; }

                [JsonProperty("pin")]
                public int? Pin { get; set; }
            }

            internal class ConfigSoilTemperature100
            {
                [JsonProperty("enabled")]
                public bool Enabled { get; set; }

                [JsonProperty("pin")]
                public int? Pin { get; set; }
            }
        }
    }
}
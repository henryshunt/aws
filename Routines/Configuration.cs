using Newtonsoft.Json;
using System.IO;

namespace AWS.Routines.Configuration
{
    internal class Configuration
    {
        public static Configuration Load(string filePath)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.MissingMemberHandling = MissingMemberHandling.Error;

            string json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<Configuration>(json, settings);
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
            [JsonProperty("rainfall")]
            public RainfallJSON Rainfall { get; set; }

            internal class RainfallJSON
            {
                [JsonProperty("enabled")]
                public bool Enabled { get; set; }
                [JsonProperty("pin")]
                public int Pin { get; set; }
            }
        }
    }
}
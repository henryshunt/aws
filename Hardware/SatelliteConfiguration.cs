namespace Aws.Hardware
{
    internal class SatelliteConfiguration
    {
        public bool WindSpeedEnabled { get; set; } = false;
        public int WindSpeedPin { get; set; }
        public bool WindDirectionEnabled { get; set; } = false;
        public int WindDirectionPin { get; set; }
        public bool SunshineDurationEnabled { get; set; } = false;
        public int SunshineDurationPin { get; set; }

        public override string ToString()
        {
            string result = "{";

            result += "\"windSpeedEnabled\":" + WindSpeedEnabled.ToString().ToLower();
            if (WindSpeedEnabled)
                result += ",\"windSpeedPin\":" + WindSpeedPin.ToString().ToLower();

            result += ",\"windDirEnabled\":" + WindDirectionEnabled.ToString().ToLower();
            if (WindDirectionEnabled)
                result += ",\"windDirPin\":" + WindDirectionPin.ToString().ToLower();

            result += ",\"sunDurEnabled\":" + SunshineDurationEnabled.ToString().ToLower();
            if (SunshineDurationEnabled)
                result += ",\"sunDurPin\":" + SunshineDurationPin.ToString().ToLower();

            return result + "}";
        }
    }
}

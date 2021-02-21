namespace Aws.Hardware
{
    internal class SatelliteConfiguration
    {
        public bool WindSpeedEnabled { get; set; } = false;
        public int WindSpeedPin { get; set; }
        public bool WindDirectionEnabled { get; set; } = false;
        public int WindDirectionPin { get; set; }

        public override string ToString()
        {
            string result = "{\"windSpeedEnabled\":" + WindSpeedEnabled.ToString().ToLower();
            if (WindSpeedEnabled)
                result += ",\"windSpeedPin\":" + WindSpeedPin.ToString().ToLower();

            result += ",\"windDirectionEnabled\":" + WindDirectionEnabled.ToString().ToLower();
            if (WindDirectionEnabled)
                result += ",\"windDirectionPin\":" + WindDirectionPin.ToString().ToLower();

            return result + "}";
        }
    }
}

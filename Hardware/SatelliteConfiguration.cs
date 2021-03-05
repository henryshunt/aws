namespace Aws.Hardware
{
    internal class SatelliteConfiguration
    {
        public bool I8paEnabled { get; set; } = false;
        public int I8paPin { get; set; }
        public bool Iev2Enabled { get; set; } = false;
        public int Iev2Pin { get; set; }

        public override string ToString()
        {
            string result = "{\"windSpeedEnabled\":" + I8paEnabled.ToString().ToLower();
            if (I8paEnabled)
                result += ",\"windSpeedPin\":" + I8paPin.ToString().ToLower();

            result += ",\"windDirectionEnabled\":" + Iev2Enabled.ToString().ToLower();
            if (Iev2Enabled)
                result += ",\"windDirectionPin\":" + Iev2Pin.ToString().ToLower();

            return result + "}";
        }
    }
}

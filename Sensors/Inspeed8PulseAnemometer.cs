namespace Aws.Sensors
{
    /// <summary>
    /// Represents an Inspeed 8-Pulse Anemometer sensor.
    /// </summary>
    internal class Inspeed8PulseAnemometer
    {
        /// <summary>
        /// The wind speed that one pulse of the sensor in one second equates to, in metres per second.
        /// </summary>
        public const double MS_PER_HZ = 0.1385824;
    }
}

using System;

namespace Aws.Sensors
{
    /// <summary>
    /// Represents a sensor.
    /// </summary>
    internal interface ISensor : IDisposable
    {
        /// <summary>
        /// Opens the sensor.
        /// </summary>
        public void Open();
    }
}

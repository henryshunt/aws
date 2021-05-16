using System;

namespace Aws.Sensors
{
    /// <summary>
    /// Represents a sensor.
    /// </summary>
    internal abstract class Sensor : IDisposable
    {
        /// <summary>
        /// Indicates whether the sensor is open.
        /// </summary>
        public bool IsOpen { get; protected set; } = false;

        /// <summary>
        /// Opens the sensor.
        /// </summary>
        public abstract void Open();

        /// <summary>
        /// Closes the sensor.
        /// </summary>
        public abstract void Close();

        public void Dispose() => Close();
    }
}

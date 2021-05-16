using System;
using System.Device.I2c;

namespace Aws.Sensors
{
    /// <summary>
    /// Represents the MCP9808 sensor.
    /// </summary>
    internal class Mcp9808 : Sensor
    {
        private Iot.Device.Mcp9808.Mcp9808 mcp9808;

        /// <summary>
        /// Initialises a new instance of the <see cref="Mcp9808"/> class.
        /// </summary>
        public Mcp9808() { }

        /// <summary>
        /// Opens the sensor.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the sensor is already open.
        /// </exception>
        public override void Open()
        {
            if (IsOpen)
                throw new InvalidOperationException("The sensor is already open");
            IsOpen = true;

            I2cDevice i2c = I2cDevice.Create(
               new I2cConnectionSettings(1, Iot.Device.Mcp9808.Mcp9808.DefaultI2cAddress));

            mcp9808 = new Iot.Device.Mcp9808.Mcp9808(i2c);
        }

        public override void Close()
        {
            mcp9808?.Dispose();
            IsOpen = false;
        }

        /// <summary>
        /// Samples the sensor.
        /// </summary>
        /// <returns>
        /// The sampled temperature in degrees celsius.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the sensor is not open.
        /// </exception>
        public double Sample()
        {
            if (!IsOpen)
                throw new InvalidOperationException("The sensor is not open");

            return mcp9808.Temperature.DegreesCelsius;
        }
    }
}

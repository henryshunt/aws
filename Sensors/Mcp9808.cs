using System.Device.I2c;

namespace Aws.Sensors
{
    /// <summary>
    /// Represents the MCP9808 sensor.
    /// </summary>
    internal class Mcp9808 : ISensor
    {
        private Iot.Device.Mcp9808.Mcp9808 mcp9808;

        /// <summary>
        /// Initialises a new instance of the <see cref="Mcp9808"/> class.
        /// </summary>
        public Mcp9808() { }

        public void Open()
        {
            I2cDevice i2c = I2cDevice.Create(
               new I2cConnectionSettings(1, Iot.Device.Mcp9808.Mcp9808.DefaultI2cAddress));

            mcp9808 = new Iot.Device.Mcp9808.Mcp9808(i2c);
        }

        /// <summary>
        /// Samples the sensor.
        /// </summary>
        /// <returns>
        /// The sampled temperature in degrees celsius.
        /// </returns>
        public double Sample()
        {
            return mcp9808.Temperature.DegreesCelsius;
        }

        public void Dispose()
        {
            mcp9808.Dispose();
        }
    }
}

using System;
using System.Device.I2c;
using System.Threading;

namespace Aws.Sensors
{
    /// <summary>
    /// Represents a HTU21D relative humidity sensor.
    /// </summary>
    internal class Htu21d : Sensor
    {
        private const byte I2C_ADDRESS = 0x40;
        private const byte CMD_READ_HUMIDITY = 0xE5;

        private I2cDevice i2cDevice;

        /// <summary>
        /// Initialises a new instance of the <see cref="Htu21d"/> class.
        /// </summary>
        public Htu21d() { }

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

            i2cDevice = I2cDevice.Create(new I2cConnectionSettings(1, I2C_ADDRESS));
        }

        public override void Close()
        {
            i2cDevice?.Dispose();
            IsOpen = false;
        }

        /// <summary>
        /// Samples the sensor.
        /// </summary>
        /// <returns>
        /// The sampled relative humidity in percent.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the sensor is not open.
        /// </exception>
        public double Sample()
        {
            if (!IsOpen)
                throw new InvalidOperationException("The sensor is not open");

            i2cDevice.WriteByte(CMD_READ_HUMIDITY);
            Thread.Sleep(16);
            Span<byte> data = stackalloc byte[3];
            i2cDevice.Read(data);

            ushort value = (ushort)(data[0] << 8);
            value |= (ushort)(data[1] & 0b_1111_1100); // Clear the two status bits

            double humidity = -6 + 125 * (value / Math.Pow(2, 16));

            if (humidity < 0)
                humidity = 0;
            else if (humidity > 100)
                humidity = 100;

            return humidity;
        }
    }
}

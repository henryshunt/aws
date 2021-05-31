using System;
using System.Device.I2c;
using UnitsNet;

namespace Aws.Sensors
{
    /// <summary>
    /// Represents the BMP280 pressure sensor.
    /// </summary>
    internal class Bmp280 : Sensor
    {
        private Iot.Device.Bmxx80.Bmp280 bmp280;

        /// <summary>
        /// Initialises a new instance of the <see cref="Bmp280"/> class.
        /// </summary>
        public Bmp280() { }

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

            I2cDevice i2cDevice = I2cDevice.Create(
                new I2cConnectionSettings(1, Iot.Device.Bmxx80.Bmp280.DefaultI2cAddress));

            bmp280 = new Iot.Device.Bmxx80.Bmp280(i2cDevice);
            bmp280.TemperatureSampling = Iot.Device.Bmxx80.Sampling.Skipped;
        }

        public override void Close()
        {
            bmp280?.Dispose();
            IsOpen = false;
        }

        /// <summary>
        /// Samples the sensor.
        /// </summary>
        /// <returns>
        /// The sampled pressure in hectopascals.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the sensor is not open.
        /// </exception>
        public double Sample()
        {
            if (!IsOpen)
                throw new InvalidOperationException("The sensor is not open");

            return bmp280.Read().Pressure.Value.Hectopascals;
        }
    }
}

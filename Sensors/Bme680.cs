using Iot.Device.Bmxx80.PowerMode;
using System;
using System.Device.I2c;
using System.Threading;
using UnitsNet;

namespace Aws.Sensors
{
    /// <summary>
    /// Represents the BME680 sensor.
    /// </summary>
    internal class Bme680 : ISensor
    {
        private Iot.Device.Bmxx80.Bme680 bme680;

        /// <summary>
        /// The number of milliseconds to wait after sampling before retrieving the sample.
        /// </summary>
        private int sampleWaitTime;

        /// <summary>
        /// Initialises a new instance of the <see cref="Bme680"/> class.
        /// </summary>
        public Bme680() { }

        /// <summary>
        /// Opens the sensor.
        /// </summary>
        public void Open()
        {
            I2cDevice i2c = I2cDevice.Create(
                new I2cConnectionSettings(1, Iot.Device.Bmxx80.Bme680.DefaultI2cAddress));

            bme680 = new Iot.Device.Bmxx80.Bme680(i2c);

            sampleWaitTime = (int)bme680.GetMeasurementDuration(bme680.HeaterProfile)
                .Milliseconds;
        }

        /// <summary>
        /// Samples the sensor.
        /// </summary>
        /// <returns>
        /// The sampled relative humidity in percent and pressure in hectopascals.
        /// </returns>
        public Tuple<double, double> Sample()
        {
            bme680.SetPowerMode(Bme680PowerMode.Forced);
            Thread.Sleep(sampleWaitTime);

            bme680.TryReadHumidity(out RelativeHumidity humidity);

            // I've had sensors go above 100% humidity, so make sure that doesn't happen
            double humidity2 = humidity.Value > 100 ? 100 : humidity.Value;

            bme680.TryReadPressure(out Pressure pressure);

            return new Tuple<double, double>(humidity2, pressure.Hectopascals);
        }

        public void Dispose()
        {
            bme680.Dispose();
        }
    }
}

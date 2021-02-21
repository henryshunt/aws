using Iot.Device.Bmxx80;
using Iot.Device.Bmxx80.PowerMode;
using System;
using System.Device.I2c;
using System.Threading;
using UnitsNet;

namespace Aws.Hardware
{
    internal class BME680
    {
        private Bme680 Device;
        private double SampleWaitTime;

        public void Initialise()
        {
            Device = new Bme680(I2cDevice.Create(new I2cConnectionSettings(1, Bme680.DefaultI2cAddress)));
            SampleWaitTime = Device.GetMeasurementDuration(Device.HeaterProfile).Milliseconds;
        }

        public Tuple<double, double, double> Sample()
        {
            Device.SetPowerMode(Bme680PowerMode.Forced);
            Thread.Sleep((int)SampleWaitTime);

            Temperature temperature = new Temperature();
            Device.TryReadTemperature(out temperature);

            RelativeHumidity humidity;
            Device.TryReadHumidity(out humidity);
            double humidity2 = humidity.Value;

            if (humidity2 > 100)
                humidity2 = 100;

            Pressure pressure = new Pressure();
            Device.TryReadPressure(out pressure);

            return new Tuple<double, double, double>(temperature.DegreesCelsius, humidity2, pressure.Hectopascals);
        }
    }
}

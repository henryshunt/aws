using Iot.Device.Bmxx80;
using Iot.Device.Bmxx80.PowerMode;
using Iot.Units;
using System;
using System.Device.I2c;
using System.Threading;

namespace AWS.Hardware
{
    internal class BME680
    {
        private Bme680 Device;
        private int SampleWaitTime;

        public void Initialise()
        {
            Device = new Bme680(I2cDevice.Create(new I2cConnectionSettings(1, Bme680.DefaultI2cAddress)));
            SampleWaitTime = Device.GetMeasurementDuration(Device.HeaterProfile);
        }

        public Tuple<double, double, double> Sample()
        {
            Device.SetPowerMode(Bme680PowerMode.Forced);
            Thread.Sleep(SampleWaitTime);

            Temperature temperature = new Temperature();
            Device.TryReadTemperature(out temperature);

            double humidity;
            Device.TryReadHumidity(out humidity);
            if (humidity > 100) humidity = 100;

            Pressure pressure = new Pressure();
            Device.TryReadPressure(out pressure);

            return new Tuple<double, double, double>(temperature.Celsius, humidity, pressure.Hectopascal);
        }
    }
}

using Iot.Device.Bmxx80;
using Iot.Device.Bmxx80.PowerMode;
using Iot.Units;
using System.Device.I2c;
using System.Threading;

namespace AWS.Hardware.Sensors
{
    internal class BME680
    {
        public bool ManualInputMode { get; private set; }

        private Bme680 Device;
        private int SampleWaitTime;

        public ListValueStore<double> TemperatureStore { get; } = new ListValueStore<double>();
        public ListValueStore<double> RelativeHumidityStore { get; } = new ListValueStore<double>();
        public ListValueStore<double> PressureStore { get; } = new ListValueStore<double>();


        public void Initialise(bool manualInputMode)
        {
            ManualInputMode = manualInputMode;

            if (!manualInputMode)
            {
                Device = new Bme680(I2cDevice.Create(new I2cConnectionSettings(1, Bme680.DefaultI2cAddress)));
                SampleWaitTime = Device.GetMeasurementDuration(Device.HeaterProfile);
            }
        }

        public Thread SampleDevice()
        {
            Thread thread = new Thread(() =>
            {
                Device.SetPowerMode(Bme680PowerMode.Forced);
                Thread.Sleep(SampleWaitTime);

                Temperature temperature = new Temperature();
                Device.TryReadTemperature(out temperature);
                TemperatureStore.ActiveValueBucket.Add(temperature.Celsius);

                double humidity;
                Device.TryReadHumidity(out humidity);
                if (humidity > 100) humidity = 100;
                RelativeHumidityStore.ActiveValueBucket.Add(humidity);

                Pressure pressure = new Pressure();
                Device.TryReadPressure(out pressure);
                PressureStore.ActiveValueBucket.Add(pressure.Hectopascal);
            });

            thread.Start();
            return thread;
        }
    }
}

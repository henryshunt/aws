using AWS.Hardware;
using AWS.Routines;
using System;
using System.Linq;

namespace AWS.Core
{
    internal class Sampler
    {
        private Configuration configuration;

        private Satellite satellite1 = new Satellite();

        private BME680 bme680 = new BME680();
        private ListValueStore<double> temperatureStore = new ListValueStore<double>();
        private ListValueStore<double> relativeHumidityStore = new ListValueStore<double>();
        private ListValueStore<double> pressureStore = new ListValueStore<double>();

        private ListValueStore<double> windSpeedStore = new ListValueStore<double>();
        private ListValueStore<double> windSpeedStore10Min = new ListValueStore<double>();
        private ListValueStore<double> windDirectionStore = new ListValueStore<double>();
        private ListValueStore<double> windDirectionStore10Min = new ListValueStore<double>();

        private RainwiseRainew111 rainGauge = new RainwiseRainew111();
        private CounterValueStore rainfallStore = new CounterValueStore();

        public Sampler(Configuration configuration)
        {
            this.configuration = configuration;
        }


        public void InitialiseSensors()
        {
            SatelliteConfiguration satellite1Config = new SatelliteConfiguration();

            if (configuration.Sensors.AirTemperature.Enabled)
                bme680.Initialise();

            if (configuration.Sensors.Satellite1.WindSpeed.Enabled)
            {
                satellite1Config.WindSpeedEnabled = true;
                satellite1Config.WindSpeedPin = (int)configuration.Sensors.Satellite1.WindSpeed.Pin;
            }

            if (configuration.Sensors.Satellite1.WindDirection.Enabled)
            {
                satellite1Config.WindDirectionEnabled = true;
                satellite1Config.WindDirectionPin = (int)configuration.Sensors.Satellite1.WindDirection.Pin;
            }

            if (configuration.Sensors.Rainfall.Enabled)
                rainGauge.Initialise((int)configuration.Sensors.Rainfall.Pin);

            satellite1.Initialise(2, satellite1Config);
        }

        public void SampleSensors(DateTime time)
        {
            Tuple<double, double, double> bme680Sample = bme680.Sample();

            temperatureStore.ActiveValueBucket.Add(bme680Sample.Item1);
            relativeHumidityStore.ActiveValueBucket.Add(bme680Sample.Item2);
            pressureStore.ActiveValueBucket.Add(bme680Sample.Item3);
        }

        public Helpers.Report GenerateReport(DateTime time)
        {
            Helpers.Report report = new Helpers.Report(time);

            report.AirTemperature = temperatureStore.InactiveValueBucket.Average();
            temperatureStore.InactiveValueBucket.Clear();
            report.RelativeHumidity = relativeHumidityStore.InactiveValueBucket.Average();
            relativeHumidityStore.InactiveValueBucket.Clear();
            report.StationPressure = pressureStore.InactiveValueBucket.Average();
            pressureStore.InactiveValueBucket.Clear();

            Console.WriteLine(string.Format("T: {0}   H: {1}   P: {2}", Math.Round((double)report.AirTemperature, 2),
                Math.Round((double)report.RelativeHumidity, 2), Math.Round((double)report.StationPressure, 2)));

            report.Rainfall = rainfallStore.InactiveValueBucket * RainwiseRainew111.MultiplicationFactor;
            rainfallStore.InactiveValueBucket = 0;

            return report;
        }
    }
}

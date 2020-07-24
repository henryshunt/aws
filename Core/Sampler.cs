using AWS.Hardware;
using AWS.Routines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

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

        private ListValueStore<KeyValuePair<DateTime, int>> windSpeedStore
            = new ListValueStore<KeyValuePair<DateTime, int>>();
        private ListValueStore<KeyValuePair<DateTime, double>> windSpeedStore10Min
            = new ListValueStore<KeyValuePair<DateTime, double>>();
        private ListValueStore<KeyValuePair<DateTime, int>> windDirectionStore
            = new ListValueStore<KeyValuePair<DateTime, int>>();
        private ListValueStore<KeyValuePair<DateTime, int>> windDirectionStore10Min
            = new ListValueStore<KeyValuePair<DateTime, int>>();

        private RainwiseRainew111 rainGauge = new RainwiseRainew111();
        private CounterValueStore rainfallStore = new CounterValueStore();

        private DateTime startTime;

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

        public void StartSensors(DateTime time)
        {
            startTime = time;
            //WindSpeedSensor.IsPaused = false;
            //RainfallSensor.IsPaused = false;
        }

        public void SampleSensors(DateTime time, bool isFirstSample)
        {
            satellite1.Sample();

            if (satellite1.LatestSample.WindSpeed != null)
            {
                windSpeedStore.ActiveValueBucket.Add(
                    new KeyValuePair<DateTime, int>(time, (int)satellite1.LatestSample.WindSpeed));
            }

            if (satellite1.LatestSample.WindDirection != null)
            {
                windDirectionStore.ActiveValueBucket.Add(
                    new KeyValuePair<DateTime, int>(time, (int)satellite1.LatestSample.WindDirection));
            }

            Tuple<double, double, double> bme680Sample = bme680.Sample();
            temperatureStore.ActiveValueBucket.Add(bme680Sample.Item1);
            relativeHumidityStore.ActiveValueBucket.Add(bme680Sample.Item2);
            pressureStore.ActiveValueBucket.Add(bme680Sample.Item3);


            if (time.Second == 59)
            {
                temperatureStore.SwapValueBucket();
                relativeHumidityStore.SwapValueBucket();
                pressureStore.SwapValueBucket();
                windDirectionStore.SwapValueBucket();
            }

            if (time.Second == 0 && !isFirstSample)
            {
                windSpeedStore.SwapValueBucket();
            }

            //Console.WriteLine(
            //    string.Format("air_temp: {0:0.00}, rel_hum: {1:0.00}, stat_pres: {2:0.00}, wind_speed: {3}, wind_dir: {4}",
            //    bme680Sample.Item1, bme680Sample.Item2, bme680Sample.Item3, satellite1.LatestSample.WindSpeed,
            //    satellite1.LatestSample.WindDirection));
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

            Console.WriteLine(string.Format("T: {0:0.00}, H: {1:0.00}, P: {2:0.00}", report.AirTemperature,
                report.RelativeHumidity, report.StationPressure));

            //report.Rainfall = rainfallStore.InactiveValueBucket * RainwiseRainew111.MMPerBucketTip;
            //rainfallStore.InactiveValueBucket = 0;

            return report;
        }
    }
}

using AWS.Hardware;
using AWS.Routines;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private Dictionary<DateTime, double> windSpeedStore10Min = new Dictionary<DateTime, double>();
        private ListValueStore<KeyValuePair<DateTime, int>> windDirectionStore
            = new ListValueStore<KeyValuePair<DateTime, int>>();
        private Dictionary<DateTime, int> windDirectionStore10Min = new Dictionary<DateTime, int>();

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

            satellite1.Initialise(1, satellite1Config);
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

            if (!isFirstSample)
            {
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
            }

            if (time.Second == 0 && !isFirstSample)
            {
                windSpeedStore.SwapValueBucket();
                windDirectionStore.SwapValueBucket();
            }

            //Console.WriteLine(
            //    string.Format("air_temp: {0:0.00}, rel_hum: {1:0.00}, stat_pres: {2:0.00}, wind_speed: {3}, wind_dir: {4}",
            //    bme680Sample.Item1, bme680Sample.Item2, bme680Sample.Item3, satellite1.LatestSample.WindSpeed,
            //    satellite1.LatestSample.WindDirection));
        }

        private Tuple<double, double, double> ProcessWindData(DateTime time)
        {
            // Add the new samples from the past minute to the 10-minute storage
            foreach (KeyValuePair<DateTime, int> kvp in windSpeedStore.InactiveValueBucket)
                windSpeedStore10Min.Add(kvp.Key, kvp.Value * 0.31);
            windSpeedStore.InactiveValueBucket.Clear();

            foreach (KeyValuePair<DateTime, int> kvp in windDirectionStore.InactiveValueBucket)
                windDirectionStore10Min.Add(kvp.Key, kvp.Value);
            windDirectionStore.InactiveValueBucket.Clear();


            // Remove samples older than 10 minutes from the 10-minute storage
            List<KeyValuePair<DateTime, double>> toRemove =
                windSpeedStore10Min.Where(kvp => kvp.Key < time - TimeSpan.FromMinutes(10)).ToList();

            foreach (var i in toRemove)
                windSpeedStore10Min.Remove(i.Key);

            List<KeyValuePair<DateTime, int>> toRemove2 =
                windDirectionStore10Min.Where(kvp => kvp.Key < time - TimeSpan.FromMinutes(10)).ToList();

            foreach (var i in toRemove)
                windDirectionStore10Min.Remove(i.Key);

            DateTime tenago = time - TimeSpan.FromMinutes(10);
            time = time - TimeSpan.FromMinutes(10);

            // Calculate wind gust
            double windGust = 0;

            for (int i = 0; i < 598; i++)
            {
                try
                {
                    var t = windSpeedStore10Min.Where(x =>
                        x.Key > time + TimeSpan.FromSeconds(i) && x.Key <= time + TimeSpan.FromSeconds(i + 3));

                    double gust = t.Average(x => x.Value);

                    if (gust > windGust)
                        windGust = gust;

                    //Console.WriteLine("sample start {0} gust {1}", time + TimeSpan.FromSeconds(i), gust);
                }
                catch { }
            }

            // Create list of vectors



            double windSpeed = windSpeedStore10Min.Average(x => x.Value);
            double windDirection = 0;

            return new Tuple<double, double, double>(windSpeed, windDirection, windGust);
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

            Tuple<double, double, double> wind = ProcessWindData(time);
            report.WindSpeed = wind.Item1;
            report.WindDirection = (int)Math.Round(wind.Item2);
            report.WindGustSpeed = wind.Item3;

            Console.WriteLine(string.Format("T: {0:0.00}, H: {1:0.00}, P: {2:0.00}, WS: {3:0.00}, WD: {4} WG: {5}",
                report.AirTemperature, report.RelativeHumidity, report.StationPressure, report.WindSpeed,
                report.WindDirection, report.WindGustSpeed));

            //report.Rainfall = rainfallStore.InactiveValueBucket * RainwiseRainew111.MMPerBucketTip;
            //rainfallStore.InactiveValueBucket = 0;

            return report;
        }
    }
}

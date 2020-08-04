using AWS.Hardware;
using AWS.Routines;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AWS.Core
{
    internal class Sampler
    {
        private Configuration configuration;
        private DateTime startTime;

        private SampleStoreAlternator sampleStore = new SampleStoreAlternator();
        private Dictionary<DateTime, double> windSpeed10Min = new Dictionary<DateTime, double>();
        private Dictionary<DateTime, int> windDirection10Min = new Dictionary<DateTime, int>();

        private Satellite satellite1 = new Satellite();
        private BME680 bme680 = new BME680();
        private RainwiseRainew111 rainGauge = new RainwiseRainew111();

        public Sampler(Configuration configuration)
        {
            this.configuration = configuration;
        }


        public void Initialise()
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

        public void Start(DateTime time)
        {
            startTime = time;

            satellite1.Start();

            //RainfallSensor.IsPaused = false;
        }

        public void Sample(DateTime time)
        {
            satellite1.Sample();

            if (satellite1.LatestSample.WindSpeed != null)
            {
                sampleStore.ActiveSampleStore.WindSpeed.Add(
                    new KeyValuePair<DateTime, int>(time, (int)satellite1.LatestSample.WindSpeed));
            }

            if (satellite1.LatestSample.WindDirection != null)
            {
                sampleStore.ActiveSampleStore.WindDirection.Add(
                    new KeyValuePair<DateTime, int>(time, (int)satellite1.LatestSample.WindDirection));
            }

            Tuple<double, double, double> bme680Sample = bme680.Sample();
            sampleStore.ActiveSampleStore.AirTemperature.Add(bme680Sample.Item1);
            sampleStore.ActiveSampleStore.RelativeHumidity.Add(bme680Sample.Item2);
            sampleStore.ActiveSampleStore.StationPressure.Add(bme680Sample.Item3);
        }

        public Report Report(DateTime time)
        {
            sampleStore.SwapSampleStore();

            Report report = new Report(time);
            report.AirTemperature = sampleStore.InactiveSampleStore.AirTemperature.Average();
            report.RelativeHumidity = sampleStore.InactiveSampleStore.RelativeHumidity.Average();
            report.StationPressure = sampleStore.InactiveSampleStore.StationPressure.Average();

            Tuple<double, double, double> windCalculations = ProcessWindData(time);
            report.WindSpeed = windCalculations.Item1;
            report.WindDirection = windCalculations.Item2;
            report.WindGustSpeed = windCalculations.Item3;

            Console.WriteLine(string.Format("T: {0:0.00}, H: {1:0.00}, P: {2:0.00}, WS: {3:0.00}, WD: {4} WG: {5:0.00}",
                report.AirTemperature, report.RelativeHumidity, report.StationPressure, report.WindSpeed,
                report.WindDirection, report.WindGustSpeed));

            sampleStore.InactiveSampleStore.Clear();
            return report;
        }

        private Tuple<double, double, double> ProcessWindData(DateTime time)
        {
            // Add the new samples from the past minute to the 10-minute storage
            foreach (KeyValuePair<DateTime, int> kvp in sampleStore.InactiveSampleStore.WindSpeed)
                windSpeed10Min.Add(kvp.Key, kvp.Value * 0.31);

            foreach (KeyValuePair<DateTime, int> kvp in sampleStore.InactiveSampleStore.WindDirection)
                windDirection10Min.Add(kvp.Key, kvp.Value);


            // Remove samples older than 10 minutes from the 10-minute storage
            List<KeyValuePair<DateTime, double>> toRemove =
                windSpeed10Min.Where(kvp => kvp.Key < time - TimeSpan.FromMinutes(10)).ToList();

            foreach (var i in toRemove)
                windSpeed10Min.Remove(i.Key);

            List<KeyValuePair<DateTime, int>> toRemove2 =
                windDirection10Min.Where(kvp => kvp.Key < time - TimeSpan.FromMinutes(10)).ToList();

            foreach (var i in toRemove)
                windDirection10Min.Remove(i.Key);

            DateTime tenago = time - TimeSpan.FromMinutes(10);
            time = time - TimeSpan.FromMinutes(10);

            // Calculate wind gust
            double windGust = 0;

            for (int i = 0; i < 598; i++)
            {
                try
                {
                    var t = windSpeed10Min.Where(x =>
                        x.Key > time + TimeSpan.FromSeconds(i) && x.Key <= time + TimeSpan.FromSeconds(i + 3));

                    double gust = t.Average(x => x.Value);

                    if (gust > windGust)
                        windGust = gust;

                    //Console.WriteLine("sample start {0} gust {1}", time + TimeSpan.FromSeconds(i), gust);
                }
                catch { }
            }

            // Create list of vectors



            double windSpeed = windSpeed10Min.Average(x => x.Value);
            double windDirection = 0;

            return new Tuple<double, double, double>(windSpeed, windDirection, windGust);
        }
    }
}

using AWS.Hardware;
using AWS.Routines;
using NLog;
using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Linq;

namespace AWS.Core
{
    internal class Sampler
    {
        private static readonly Logger eventLogger = LogManager.GetCurrentClassLogger();

        private Configuration config;
        private GpioController gpio;

        private DateTime startTime;

        private SampleStoreAlternator sampleStore = new SampleStoreAlternator();
        private Dictionary<DateTime, double> windSpeed10Min = new Dictionary<DateTime, double>();
        private Dictionary<DateTime, int> windDirection10Min = new Dictionary<DateTime, int>();

        private Satellite satellite1 = new Satellite();
        private BME680 bme680 = new BME680();
        private RainwiseRainew111 rainGauge = new RainwiseRainew111();

        public Sampler(Configuration config, GpioController gpio)
        {
            this.config = config;
            this.gpio = gpio;
        }


        public bool Initialise()
        {
            if (config.Sensors.AirTemperature.Enabled)
            {
                try
                {
                    bme680.Initialise();
                }
                catch (Exception ex)
                {
                    eventLogger.Error(ex, "Failed to initialise BME680 sensor");
                    return false;
                }
            }

            if (config.Sensors.Satellite1.Enabled)
            {
                SatelliteConfiguration satelliteConfig = new SatelliteConfiguration();

                if (config.Sensors.Satellite1.WindSpeed.Enabled)
                {
                    satelliteConfig.WindSpeedEnabled = true;
                    satelliteConfig.WindSpeedPin = (int)config.Sensors.Satellite1.WindSpeed.Pin;
                }

                if (config.Sensors.Satellite1.WindDirection.Enabled)
                {
                    satelliteConfig.WindDirectionEnabled = true;
                    satelliteConfig.WindDirectionPin = (int)config.Sensors.Satellite1.WindDirection.Pin;
                }

                try
                {
                    if (!satellite1.Initialise(1, satelliteConfig))
                    {
                        eventLogger.Error("Failed to initialise satellite device 1");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    eventLogger.Error(ex, "Failed to initialise satellite device 1");
                    return false;
                }
            }

            if (config.Sensors.Rainfall.Enabled)
            {
                try
                {
                    rainGauge.Initialise(gpio, (int)config.Sensors.Rainfall.Pin);
                }
                catch (Exception ex)
                {
                    eventLogger.Error(ex, "Failed to initialise rain gauge");
                    return false;
                }
            }

            return true;
        }

        public void Start(DateTime time)
        {
            startTime = time;

            if (config.Sensors.Satellite1.Enabled)
                satellite1.Start();

            if (config.Sensors.Rainfall.Enabled)
                rainGauge.Start();
        }

        public bool Sample(DateTime time)
        {
            if (config.Sensors.Satellite1.Enabled && satellite1.Sample())
            {
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
            }

            Tuple<double, double, double> bme680Sample = bme680.Sample();
            sampleStore.ActiveSampleStore.AirTemperature.Add(bme680Sample.Item1);
            sampleStore.ActiveSampleStore.RelativeHumidity.Add(bme680Sample.Item2);
            sampleStore.ActiveSampleStore.StationPressure.Add(bme680Sample.Item3);

            if (config.Sensors.Rainfall.Enabled)
                sampleStore.ActiveSampleStore.Rainfall.Add(rainGauge.Sample());

            return true;
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

            report.Rainfall = sampleStore.InactiveSampleStore.Rainfall.Sum();

            Console.WriteLine(string.Format(
                "T:{0:0.00}, H:{1:0.00}, P:{2:0.00}, WS:{3:0.00}, WD:{4}, WG:{5:0.00}, R:{6:0.000}",
                report.AirTemperature, report.RelativeHumidity, report.StationPressure, report.WindSpeed,
                report.WindDirection, report.WindGustSpeed, report.Rainfall));

            sampleStore.InactiveSampleStore.Clear();
            return report;
        }

        private Tuple<double, double, double> ProcessWindData(DateTime time)
        {
            // Add the new samples from the past minute to the 10-minute storage
            foreach (KeyValuePair<DateTime, int> kvp in sampleStore.InactiveSampleStore.WindSpeed)
                windSpeed10Min.Add(kvp.Key, kvp.Value * Inspeed8PulseAnemometer.WindSpeedMphPerHz);

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

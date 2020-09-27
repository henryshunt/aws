using AWS.Hardware;
using AWS.Routines;
using NLog;
using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Linq;
using System.Numerics;

namespace AWS.Core
{
    internal class Sampler
    {
        private static readonly Logger eventLogger = LogManager.GetCurrentClassLogger();

        private Configuration config;
        private GpioController gpio;

        private bool hasConnected = false;
        private bool hasStarted = false;
        private DateTime startTime;

        private readonly SampleStoreAlternator sampleStore = new SampleStoreAlternator();
        private readonly List<KeyValuePair<DateTime, double>> windSpeed10MinStore = new List<KeyValuePair<DateTime, double>>();
        private readonly List<KeyValuePair<DateTime, int>> windDirection10MinStore = new List<KeyValuePair<DateTime, int>>();

        private Satellite satellite1 = new Satellite();
        private BME680 bme680 = new BME680();
        private RainwiseRainew111 rainGauge = new RainwiseRainew111();

        public Sampler(Configuration config, GpioController gpio)
        {
            this.config = config;
            this.gpio = gpio;
        }


        /// <summary>
        /// Configures and opens a connection to the sensors marked as enabled in the configuration.
        /// </summary>
        /// <returns>An indication of success or failure.</returns>
        public bool Connect()
        {
            if (config.sensors.airTemperature.enabled == true || config.sensors.relativeHumidity.enabled == true ||
                config.sensors.barometricPressure.Enabled == true)
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

            if (config.sensors.satellite.enabled == true)
            {
                SatelliteConfiguration satelliteConfig = new SatelliteConfiguration();

                if (config.sensors.satellite.windSpeed.enabled == true)
                {
                    satelliteConfig.WindSpeedEnabled = true;
                    satelliteConfig.WindSpeedPin = (int)config.sensors.satellite.windSpeed.pin;
                }

                if (config.sensors.satellite.windDirection.enabled == true)
                {
                    satelliteConfig.WindDirectionEnabled = true;
                    satelliteConfig.WindDirectionPin = (int)config.sensors.satellite.windDirection.pin;
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

            if (config.sensors.rainfall.enabled == true)
            {
                try
                {
                    rainGauge.Initialise(gpio, (int)config.sensors.rainfall.pin);
                }
                catch (Exception ex)
                {
                    eventLogger.Error(ex, "Failed to initialise rain gauge");
                    return false;
                }
            }

            hasConnected = true;
            return true;
        }

        /// <summary>
        /// Some sensors (such as those which work by counting events) require being explicitly
        /// started. This method starts those sensors.
        /// </summary>
        /// <param name="time">Time that the sensors were started.</param>
        public bool Start(DateTime time)
        {
            if (!hasConnected)
                throw new WorkflowOrderException("You must call Connect() first.");

            startTime = time;

            if (config.sensors.satellite.enabled == true)
                satellite1.Start();

            if (config.sensors.rainfall.enabled == true)
                rainGauge.Start();

            hasStarted = true;
            return true;
        }

        /// <summary>
        /// Samples the sensors marked as enabled in the configuration and stores the values internally.
        /// </summary>
        /// <param name="time">Time of the sample.</param>
        /// <returns>An indication of success or failure.</returns>
        public bool Sample(DateTime time)
        {
            if (!hasConnected)
                throw new WorkflowOrderException("You must call Connect() first.");
            else if (!hasStarted)
                throw new WorkflowOrderException("You must call Start() first.");

            // Sample the satellite first since it resets the wind speed counter
            if (config.sensors.satellite.enabled == true && satellite1.Sample())
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
            sampleStore.ActiveSampleStore.BarometricPressure.Add(bme680Sample.Item3);

            if (config.sensors.rainfall.enabled == true)
                sampleStore.ActiveSampleStore.Rainfall.Add(rainGauge.Sample());

            return true;
        }

        /// <summary>
        /// Produces a report from the samples stored since the last time this method was called.
        /// </summary>
        /// <param name="time">Time of the report.</param>
        /// <returns>The produced report.</returns>
        public Report Report(DateTime time)
        {
            sampleStore.SwapSampleStore();
            Report report = new Report(time);

            if (sampleStore.InactiveSampleStore.AirTemperature.Count > 0)
                report.AirTemperature = sampleStore.InactiveSampleStore.AirTemperature.Average();

            if (sampleStore.InactiveSampleStore.RelativeHumidity.Count > 0)
                report.RelativeHumidity = sampleStore.InactiveSampleStore.RelativeHumidity.Average();

            Update10MinuteWindStore(time);
            Tuple<double, double, double> windValues = CalculateWindValues(time);

            report.WindSpeed = windValues.Item1;
            report.WindDirection = windValues.Item2;
            report.WindGust = windValues.Item3;

            if (config.sensors.rainfall.enabled == true)
            {
                if (sampleStore.InactiveSampleStore.Rainfall.Count > 0)
                    report.Rainfall = sampleStore.InactiveSampleStore.Rainfall.Sum();
                else report.Rainfall = 0;
            }

            if (sampleStore.InactiveSampleStore.BarometricPressure.Count > 0)
                report.BarometricPressure = sampleStore.InactiveSampleStore.BarometricPressure.Average();

            if (config.sensors.sunshineDuration.enabled == true)
            {
                if (sampleStore.InactiveSampleStore.SunshineDuration.Count > 0)
                    report.SunshineDuration = sampleStore.InactiveSampleStore.SunshineDuration.Sum();
                else report.SunshineDuration = 0;
            }

            if (sampleStore.InactiveSampleStore.SoilTemperature10.Count > 0)
                report.SoilTemperature10 = sampleStore.InactiveSampleStore.SoilTemperature10.Average();

            if (sampleStore.InactiveSampleStore.SoilTemperature30.Count > 0)
                report.SoilTemperature30 = sampleStore.InactiveSampleStore.SoilTemperature30.Average();

            if (sampleStore.InactiveSampleStore.SoilTemperature100.Count > 0)
                report.SoilTemperature100 = sampleStore.InactiveSampleStore.SoilTemperature100.Average();

            Console.WriteLine(report.ToString());

            sampleStore.InactiveSampleStore.Clear();
            return report;
        }

        /// <summary>
        /// Copies wind speed and direction samples from the inactive sample store to the ten-minute
        /// wind store and removes samples older than ten minutes from the ten-minute wind store.
        /// </summary>
        /// <param name="tenMinuteEnd">The end of the ten-minute period.</param>
        private void Update10MinuteWindStore(DateTime tenMinuteEnd)
        {
            foreach (KeyValuePair<DateTime, int> sample in sampleStore.InactiveSampleStore.WindSpeed)
            {
                windSpeed10MinStore.Add(new KeyValuePair<DateTime, double>(
                    sample.Key, sample.Value * Inspeed8PulseAnemometer.WindSpeedMphPerHz));
            }

            windDirection10MinStore.AddRange(sampleStore.InactiveSampleStore.WindDirection);

            // 599 is 10 minutes minus 1 second
            windSpeed10MinStore.RemoveAll(sample => sample.Key < tenMinuteEnd - TimeSpan.FromSeconds(599));
            windDirection10MinStore.RemoveAll(sample => sample.Key < tenMinuteEnd - TimeSpan.FromSeconds(599));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tenMinuteEnd">The end of the ten-minute period.</param>
        /// <returns>A tuple containing wind speed, direction and gust.</returns>
        private Tuple<double, double, double> CalculateWindValues(DateTime tenMinuteEnd)
        {
            double windGust = 0;

            for (DateTime i = tenMinuteEnd - TimeSpan.FromSeconds(599);
                i <= tenMinuteEnd - TimeSpan.FromSeconds(2); i += TimeSpan.FromSeconds(1))
            {
                //try
                //{
                //    var t = windSpeed10MinStore.Where(x =>
                //        x.Key > tenMinuteEnd + TimeSpan.FromSeconds(i) && x.Key <= tenMinuteEnd + TimeSpan.FromSeconds(i + 3));

                //    double gust = t.Average(x => x.Value);

                //    if (gust > windGust)
                //        windGust = gust;

                //Console.WriteLine("sample start {0} to {1}", i, i + TimeSpan.FromSeconds(3));
                //}
                //catch { }
            }

            // Create list of vectors
            //List<Tuple<int, double>> vectors = new List<Tuple<int, double>>();

            //for (DateTime i = tenMinuteEnd - TimeSpan.FromSeconds(599); i <= tenMinuteEnd; i += TimeSpan.FromSeconds(1))
            //{
            //    if (windSpeed10MinStore.Any(sample => sample.Key == i) &&
            //        windDirection10MinStore.Any(sample => sample.Key == i))
            //    {
            //        vectors.Add(new Tuple<int, double>());
            //    }
            //}

            double windSpeed = 0; //windSpeed10MinStore.Average(x => x.Value);
            double windDirection = 0;

            return new Tuple<double, double, double>(windSpeed, windDirection, windGust);
        }
    }
}

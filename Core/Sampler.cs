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

        private Mcp9808 mcp9808 = new Mcp9808();
        private BME680 bme680 = new BME680();
        private Satellite satellite = new Satellite();
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
            if (config.sensors.airTemperature.enabled == true)
            {
                try
                {
                    mcp9808.Initialise();
                }
                catch (Exception ex)
                {
                    eventLogger.Error(ex, "Failed to initialise MCP9808 sensor");
                    return false;
                }
            }

            if (config.sensors.relativeHumidity.enabled == true || config.sensors.barometricPressure.Enabled == true)
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
                    if (!satellite.Initialise(1, satelliteConfig))
                    {
                        eventLogger.Error("Failed to initialise satellite device");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    eventLogger.Error(ex, "Failed to initialise satellite device");
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
                satellite.Start();

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
            if (config.sensors.satellite.enabled == true && satellite.Sample())
            {
                if (satellite.LatestSample.WindSpeed != null)
                {
                    sampleStore.ActiveSampleStore.WindSpeed.Add(
                        new KeyValuePair<DateTime, int>(time, (int)satellite.LatestSample.WindSpeed));
                }

                if (satellite.LatestSample.WindDirection != null)
                {
                    sampleStore.ActiveSampleStore.WindDirection.Add(
                        new KeyValuePair<DateTime, int>(time, (int)satellite.LatestSample.WindDirection));
                }
            }

            if (config.sensors.airTemperature.enabled == true)
                sampleStore.ActiveSampleStore.AirTemperature.Add(mcp9808.Sample());

            Tuple<double, double, double> bme680Sample = bme680.Sample();
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
            (double?, double?, double?) windValues = CalculateWindValues(time);

            report.WindSpeed = windValues.Item1;
            report.WindGust = windValues.Item2;
            report.WindDirection = windValues.Item3;

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
            DateTime tenMinuteStart = tenMinuteEnd - TimeSpan.FromMinutes(10);

            if (config.sensors.satellite.windSpeed.enabled == true)
            {
                foreach (KeyValuePair<DateTime, int> sample in sampleStore.InactiveSampleStore.WindSpeed)
                {
                    windSpeed10MinStore.Add(new KeyValuePair<DateTime, double>(sample.Key,
                        sample.Value * Inspeed8PulseAnemometer.WindSpeedMphPerHz));
                }

                windSpeed10MinStore.RemoveAll(sample => sample.Key <= tenMinuteStart);
            }

            if (config.sensors.satellite.windDirection.enabled == true)
            {
                windDirection10MinStore.AddRange(sampleStore.InactiveSampleStore.WindDirection);
                windDirection10MinStore.RemoveAll(sample => sample.Key <= tenMinuteStart);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tenMinuteEnd">The end of the ten-minute period.</param>
        /// <returns>A tuple containing wind speed, direction and gust.</returns>
        private (double?, double?, double?) CalculateWindValues(DateTime tenMinuteEnd)
        {
            double? windSpeed;
            if (config.sensors.satellite.windSpeed.enabled == true && windSpeed10MinStore.Count > 0)
                windSpeed = windSpeed10MinStore.Average(x => x.Value);
            else windSpeed = null;

            double? windGust = null;
            if (config.sensors.satellite.windSpeed.enabled == true && windSpeed10MinStore.Count > 0)
            {
                windGust = 0;

                // Find the highest 3-second average wind speed in the stored data. A 3-second average
                // includes the samples <= second T and > T-3
                for (DateTime i = tenMinuteEnd - TimeSpan.FromMinutes(10);
                    i <= tenMinuteEnd - TimeSpan.FromSeconds(3); i += TimeSpan.FromSeconds(1))
                {
                    var gustSamples = windSpeed10MinStore.Where(x => x.Key > i && x.Key <= i + TimeSpan.FromSeconds(3));

                    if (gustSamples.Count() > 0)
                    {
                        double gustSample = gustSamples.Average(x => x.Value);

                        if (gustSample > windGust)
                            windGust = gustSample;
                    }
                }
            }

            double? windDirection = null;
            if (config.sensors.satellite.windDirection.enabled == true && windSpeed != null && windSpeed > 0 &&
                windDirection10MinStore.Count > 0)
            {
                List<(double, double)> vectors = new List<(double, double)>();

                // Create a vector (speed and direction pair) for each second in the 10-minute period
                for (DateTime i = tenMinuteEnd - TimeSpan.FromSeconds(599); i <= tenMinuteEnd; i += TimeSpan.FromSeconds(1))
                {
                    if (windSpeed10MinStore.Any(sample => sample.Key == i) &&
                        windDirection10MinStore.Any(sample => sample.Key == i))
                    {
                        vectors.Add((windSpeed10MinStore.Single(sample => sample.Key == i).Value,
                            windDirection10MinStore.Single(sample => sample.Key == i).Value));
                    }
                }

                if (vectors.Count > 0)
                    windDirection = Helpers.AverageWindDirection(vectors);
            }

            return (windSpeed, windGust, windDirection);
        }
    }
}

using Aws.Hardware;
using Aws.Routines;
using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Aws.Core
{
    /// <summary>
    /// Represents the AWS subsystem responsible for collecting and logging sensor data.
    /// </summary>
    internal class DataLogger
    {
        /// <summary>
        /// The AWS' configuration data.
        /// </summary>
        private readonly Configuration config;

        /// <summary>
        /// The GPIO controller.
        /// </summary>
        private readonly GpioController gpio;

        /// <summary>
        /// Indicates whether the data logger is open and connected to the sensors.
        /// </summary>
        private bool isOpen = false;

        /// <summary>
        /// Indicates whether the data logger has started sampling from the sensors.
        /// </summary>
        private bool isSampling = false;

        /// <summary>
        /// The time the data logger started sampling from the sensors.
        /// </summary>
        private DateTime startTime;

        /// <summary>
        /// Stores the sensor samples taken between the previous and next reports (a duration of
        /// one minute).
        /// </summary>
        private SampleStore sampleStore = new SampleStore();

        /// <summary>
        /// Stores the past ten minutes of wind speed samples.
        /// </summary>
        private readonly List<KeyValuePair<DateTime, double>> windSpeed10MinStore
            = new List<KeyValuePair<DateTime, double>>();

        /// <summary>
        /// Stores the past ten minutes of wind direction samples.
        /// </summary>
        private readonly List<KeyValuePair<DateTime, int>> windDirection10MinStore
            = new List<KeyValuePair<DateTime, int>>();

        #region Sensors
        private Mcp9808 mcp9808 = null;
        private BME680 bme680 = null;
        private Satellite satellite = null;
        private RainwiseRainew111 rr111 = null;
        #endregion

        /// <summary>
        /// Initialises a new instance of the DataLogger class.
        /// </summary>
        /// <param name="config">The AWS' configuration data.</param>
        /// <param name="gpio">The GPIO controller.</param>
        public DataLogger(Configuration config, GpioController gpio)
        {
            this.config = config;
            this.gpio = gpio;
        }

        /// <summary>
        /// Opens a connection to the sensors marked as enabled in the AWS' configuration.
        /// </summary>
        public void Open()
        {
            if (isOpen)
                throw new InvalidOperationException(nameof(DataLogger) + " is already open");

            if (config.sensors.mcp9808.enabled == true)
            {
                try
                {
                    mcp9808 = new Mcp9808();
                    mcp9808.Open();
                }
                catch
                {
                    Helpers.LogEvent(null, nameof(DataLogger), "Failed to open MCP9808 sensor");
                    throw new DataLoggerException("Failed to open a sensor");
                }
            }

            if (config.sensors.bme680.enabled == true)
            {
                try
                {
                    bme680 = new BME680();
                    bme680.Open();
                }
                catch
                {
                    Helpers.LogEvent(null, nameof(DataLogger), "Failed to open BME680 sensor");
                    throw new DataLoggerException("Failed to open a sensor");
                }
            }

            if (config.sensors.satellite.enabled == true)
            {
                SatelliteConfiguration satConfig = new SatelliteConfiguration();

                if (config.sensors.satellite.i8pa.enabled == true)
                {
                    satConfig.I8paEnabled = true;
                    satConfig.I8paPin = (int)config.sensors.satellite.i8pa.pin;
                }

                if (config.sensors.satellite.iev2.enabled == true)
                {
                    satConfig.Iev2Enabled = true;
                    satConfig.Iev2Pin = (int)config.sensors.satellite.iev2.pin;
                }

                try
                {
                    satellite = new Satellite(1, satConfig);
                    satellite.Open();
                }
                catch
                {
                    Helpers.LogEvent(null, nameof(DataLogger), "Failed to open satellite sensor");
                    throw new DataLoggerException("Failed to open a sensor");
                }
            }

            if (config.sensors.rr111.enabled == true)
            {
                try
                {
                    rr111 = new RainwiseRainew111(gpio, (int)config.sensors.rr111.pin);
                    rr111.Open();
                }
                catch
                {
                    Helpers.LogEvent(null, nameof(DataLogger), "Failed to open RR111 sensor");
                    throw new DataLoggerException("Failed to open a sensor");
                }
            }

            isOpen = true;
            Helpers.LogEvent(null, nameof(DataLogger), "Opened connection to sensors");
        }

        /// <summary>
        /// This method should be called once per second. It essentially keeps the data logger
        /// going.
        /// </summary>
        /// <param name="time">The time of the tick.</param>
        public void Tick(DateTime time)
        {
            if (!isOpen)
                throw new InvalidOperationException(nameof(DataLogger) + " is not open");

            if (!isSampling)
            {
                // Start sampling at the top of the minute
                if (time.Second == 0)
                {
                    try
                    {
                        if (config.sensors.satellite.enabled == true)
                            satellite.Start();
                        if (config.sensors.rr111.enabled == true)
                            rr111.Start();

                        startTime = time;
                        isSampling = true;
                        Helpers.LogEvent(time, nameof(DataLogger), "Started sampling");
                    }
                    catch
                    {
                        gpio.Write(config.errorLedPin, PinValue.High);
                        Helpers.LogEvent(time, nameof(DataLogger), "Failed to start sampling");
                        throw new DataLoggerException("Failed to start sampling");
                    }
                }
                else
                {
                    // Flash the LED until sampling starts
                    gpio.Write(config.dataLedPin, PinValue.High);
                    Thread.Sleep(500);
                    gpio.Write(config.dataLedPin, PinValue.Low);
                }

                return;
            }

            Sample(time);

            if (time.Second == 0)
                Log(time);
        }

        /// <summary>
        /// Samples the sensors marked as enabled in the AWS' configuration and adds the values to
        /// <see cref="sampleStore"/>.
        /// </summary>
        /// <param name="time">The time of the sample.</param>
        private void Sample(DateTime time)
        {
            if (config.sensors.mcp9808.enabled == true)
            {
                try
                {
                    sampleStore.AirTemperature.Add(mcp9808.Sample());
                }
                catch
                {
                    gpio.Write(config.errorLedPin, PinValue.High);
                    Helpers.LogEvent(time, nameof(DataLogger), "Failed to sample mcp9808 sensor");
                }
            }

            if (config.sensors.bme680.enabled == true)
            {
                try
                {
                    Tuple<double, double, double> bme680Sample = bme680.Sample();
                    sampleStore.RelativeHumidity.Add(bme680Sample.Item2);
                    sampleStore.BarometricPressure.Add(bme680Sample.Item3);
                }
                catch
                {
                    gpio.Write(config.errorLedPin, PinValue.High);
                    Helpers.LogEvent(time, nameof(DataLogger), "Failed to sample bme680 sensor");
                }
            }

            if (config.sensors.satellite.enabled == true)
            {
                try
                {
                    SatelliteSample sample = satellite.Sample();

                    if (config.sensors.satellite.i8pa.enabled == true &&
                        sample.WindSpeed != null)
                    {
                        sampleStore.WindSpeed.Add(new KeyValuePair<DateTime, double>(
                            time, ((int)sample.WindSpeed) * Inspeed8PulseAnemom.WindSpeedMphPerHz));
                    }

                    if (config.sensors.satellite.iev2.enabled == true &&
                        sample.WindDirection != null)
                    {
                        sampleStore.WindDirection.Add(new KeyValuePair<DateTime, int>(
                            time, (int)sample.WindDirection));
                    }
                }
                catch
                {
                    gpio.Write(config.errorLedPin, PinValue.High);
                    Helpers.LogEvent(time, nameof(DataLogger), "Failed to sample satellite sensor");
                }
            }

            if (config.sensors.rr111.enabled == true)
            {
                try
                {
                    sampleStore.Rainfall.Add(rr111.Sample());
                }
                catch
                {
                    gpio.Write(config.errorLedPin, PinValue.High);
                    Helpers.LogEvent(time, nameof(DataLogger), "Failed to sample rr111 sensor");
                }
            }
        }

        /// <summary>
        /// Generates and logs a report to the database.
        /// </summary>
        /// <param name="time">The time of the report.</param>
        private void Log(DateTime time)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();

            gpio.Write(config.dataLedPin, PinValue.High);
            gpio.Write(config.errorLedPin, PinValue.Low);

            // Cache the sample store for use here and clear the original store
            SampleStore store = sampleStore;
            sampleStore = new SampleStore();

            new Thread(() =>
            {
                UpdateWindStores(time, store);
                Database.WriteReport(GenerateReport(time, store));

                // Keep the LED on for at least 1.5 seconds
                timer.Stop();
                if (timer.ElapsedMilliseconds < 1500)
                    Thread.Sleep(1500 - (int)timer.ElapsedMilliseconds);

                gpio.Write(config.dataLedPin, PinValue.Low);
            }).Start();
        }

        /// <summary>
        /// Produces a report from a collection of samples.
        /// </summary>
        /// <param name="time">Time of the report.</param>
        /// <param name="store">The sample store containing the data to calculate the report from.</param>
        /// <returns>The produced report.</returns>
        private Report GenerateReport(DateTime time, SampleStore store)
        {
            Report report = new Report(time);

            if (store.AirTemperature.Count > 0)
                report.AirTemperature = Math.Round(store.AirTemperature.Average(), 1);

            if (store.RelativeHumidity.Count > 0)
                report.RelativeHumidity = Math.Round(store.RelativeHumidity.Average(), 1);

            // Need at least 10 minutes of wind data
            if (time >= startTime + TimeSpan.FromMinutes(10))
            {
                (double?, double?, int?) windValues = CalculateWindValues(time);

                if (windValues.Item1 != null)
                    report.WindSpeed = Math.Round((double)windValues.Item1, 1);
                if (windValues.Item2 != null)
                    report.WindGust = Math.Round((double)windValues.Item2, 1);
                if (windValues.Item3 != null)
                    report.WindDirection = (int)windValues.Item3;
            }

            if (config.sensors.rr111.enabled == true)
            {
                if (store.Rainfall.Count > 0)
                    report.Rainfall = store.Rainfall.Sum();
                else report.Rainfall = 0;
            }

            if (store.BarometricPressure.Count > 0)
                report.BarometricPressure = Math.Round(store.BarometricPressure.Average());

            if (report.AirTemperature != null && report.RelativeHumidity != null)
            {
                report.DewPoint = Helpers.CalculateDewPoint(
                    (double)report.AirTemperature, (double)report.RelativeHumidity);
            }

            Console.WriteLine(report.ToString());
            return report;
        }

        /// <summary>
        /// Copies wind speed and direction samples from a sample store to the ten-minute
        /// wind stores and removes samples older than ten minutes from the ten-minute wind stores.
        /// </summary>
        /// <param name="tenMinuteEnd">The end of the ten-minute period.</param>
        /// <param name="store">The sample store containing the new samples.</param>
        private void UpdateWindStores(DateTime tenMinuteEnd, SampleStore store)
        {
            DateTime tenMinuteStart = tenMinuteEnd - TimeSpan.FromMinutes(10);

            if (config.sensors.satellite.i8pa.enabled == true)
            {
                windSpeed10MinStore.AddRange(store.WindSpeed);
                windSpeed10MinStore.RemoveAll(sample => sample.Key <= tenMinuteStart);
                store.WindSpeed.Clear();
            }

            if (config.sensors.satellite.iev2.enabled == true)
            {
                windDirection10MinStore.AddRange(store.WindDirection);
                windDirection10MinStore.RemoveAll(sample => sample.Key <= tenMinuteStart);
                store.WindDirection.Clear();
            }
        }

        /// <summary>
        /// Calculates average wind speed and direction, and maximum 3-second wind gust, over the
        /// past ten minutes.
        /// </summary>
        /// <param name="tenMinuteEnd">The end of the ten-minute period.</param>
        /// <returns>A tuple containing wind speed, gust and direction.</returns>
        private (double?, double?, int?) CalculateWindValues(DateTime tenMinuteEnd)
        {
            double? windSpeed = null;
            if (config.sensors.satellite.i8pa.enabled == true && windSpeed10MinStore.Count > 0)
                windSpeed = windSpeed10MinStore.Average(x => x.Value);

            double? windGust = null;
            if (config.sensors.satellite.i8pa.enabled == true && windSpeed10MinStore.Count > 0)
            {
                windGust = 0;

                // Find the highest 3-second average wind speed in the stored data. A 3-second average
                // includes the samples <= second T and > T-3
                for (DateTime i = tenMinuteEnd - TimeSpan.FromMinutes(10);
                    i <= tenMinuteEnd - TimeSpan.FromSeconds(3); i += TimeSpan.FromSeconds(1))
                {
                    var gustSamples = windSpeed10MinStore.Where
                        (x => x.Key > i && x.Key <= i + TimeSpan.FromSeconds(3));

                    if (gustSamples.Count() > 0)
                    {
                        double gustSample = gustSamples.Average(x => x.Value);

                        if (gustSample > windGust)
                            windGust = gustSample;
                    }
                }
            }

            int? windDirection = null;
            if (config.sensors.satellite.iev2.enabled == true && windSpeed != null && windSpeed > 0 &&
                windDirection10MinStore.Count > 0)
            {
                List<Vector> vectors = new List<Vector>();

                // Create a vector (speed and direction pair) for each second in the 10-minute period
                for (DateTime i = tenMinuteEnd - TimeSpan.FromSeconds(599);
                    i <= tenMinuteEnd; i += TimeSpan.FromSeconds(1))
                {
                    if (windSpeed10MinStore.Any(sample => sample.Key == i) &&
                        windDirection10MinStore.Any(sample => sample.Key == i))
                    {
                        double magnitude = windSpeed10MinStore.Single(sample => sample.Key == i).Value;
                        double direction = windDirection10MinStore.Single(sample => sample.Key == i).Value;
                        vectors.Add(new Vector(magnitude, direction));
                    }
                }

                if (vectors.Count > 0)
                    windDirection = ((int)Math.Round(Helpers.AverageWindDirection(vectors))) % 360;
            }

            return (windSpeed, windGust, windDirection);
        }
    }
}

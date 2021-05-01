using Aws.Hardware;
using Aws.Routines;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Aws.Core
{
    /// <summary>
    /// Represents the subsystem responsible for collecting and logging sensor data.
    /// </summary>
    internal class DataLogger
    {
        /// <summary>
        /// The configuration data.
        /// </summary>
        private readonly Configuration config;

        /// <summary>
        /// The GPIO controller.
        /// </summary>
        private readonly GpioController gpio;

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
        private Bme680 bme680 = null;
        private Satellite satellite = null;
        private RainwiseRainew111 rr111 = null;
        #endregion

        public event EventHandler StartFailed;
        public event EventHandler<DataLoggerEventArgs> DataLogged;

        /// <summary>
        /// Initialises a new instance of the <see cref="DataLogger"/> class.
        /// </summary>
        /// <param name="config">
        /// The configuration data.
        /// </param>
        /// <param name="gpio">
        /// The GPIO controller.
        /// </param>
        public DataLogger(Configuration config, GpioController gpio)
        {
            this.config = config;
            this.gpio = gpio;
        }

        /// <summary>
        /// Opens a connection to the sensors enabled in the configuration.
        /// </summary>
        public void Open()
        {
            bool success = true;

            if ((bool)config.sensors.mcp9808.enabled)
            {
                try
                {
                    mcp9808 = new Mcp9808();
                    mcp9808.Open();
                }
                catch
                {
                    gpio.Write(config.errorLedPin, PinValue.High);
                    Helpers.LogEvent("Failed to open MCP9808 sensor");
                    success = false;
                }
            }

            if ((bool)config.sensors.bme680.enabled)
            {
                try
                {
                    bme680 = new Bme680();
                    bme680.Open();
                }
                catch
                {
                    gpio.Write(config.errorLedPin, PinValue.High);
                    Helpers.LogEvent("Failed to open BME680 sensor");
                    success = false;
                }
            }

            if ((bool)config.sensors.satellite.enabled)
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
                    gpio.Write(config.errorLedPin, PinValue.High);
                    Helpers.LogEvent("Failed to open satellite sensor");
                    success = false;
                }
            }

            if ((bool)config.sensors.rr111.enabled)
            {
                try
                {
                    rr111 = new RainwiseRainew111(gpio, (int)config.sensors.rr111.pin);
                    rr111.Open();
                }
                catch
                {
                    gpio.Write(config.errorLedPin, PinValue.High);
                    Helpers.LogEvent("Failed to open RR111 sensor");
                    success = false;
                }
            }

            if (!success)
            {
                for (int i = 0; i < 5; i++)
                {
                    gpio.Write(config.errorLedPin, PinValue.High);
                    Thread.Sleep(250);
                    gpio.Write(config.errorLedPin, PinValue.Low);
                    Thread.Sleep(250);
                }
            }
        }

        /// <summary>
        /// This is the main method that keeps the data logger working. It should be called once per second.
        /// </summary>
        /// <param name="time">
        /// The time of the tick.
        /// </param>
        public void Tick(DateTime time)
        {
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
                    }
                    catch
                    {
                        gpio.Write(config.errorLedPin, PinValue.High);
                        Helpers.LogEvent("Failed to start sampling");
                        StartFailed?.Invoke(this, new EventArgs());
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
        /// Reads each of the sensors enabled in the configuration and stores the values in
        /// <see cref="sampleStore"/>.
        /// </summary>
        /// <param name="time">
        /// The time of the sample.
        /// </param>
        private void Sample(DateTime time)
        {
            if ((bool)config.sensors.mcp9808.enabled)
            {
                try { sampleStore.AirTemperature.Add(mcp9808.Sample()); }
                catch
                {
                    gpio.Write(config.errorLedPin, PinValue.High);
                    Helpers.LogEvent("Failed to sample MCP9808 sensor");
                }
            }

            if ((bool)config.sensors.bme680.enabled)
            {
                try
                {
                    Tuple<double, double, double> sample = bme680.Sample();
                    sampleStore.RelativeHumidity.Add(sample.Item2);
                    sampleStore.StationPressure.Add(sample.Item3);
                }
                catch
                {
                    gpio.Write(config.errorLedPin, PinValue.High);
                    Helpers.LogEvent("Failed to sample BME680 sensor");
                }
            }

            if ((bool)config.sensors.satellite.enabled)
            {
                try
                {
                    SatelliteSample sample = satellite.Sample();

                    if ((bool)config.sensors.satellite.i8pa.enabled && sample.WindSpeed != null)
                    {
                        sampleStore.WindSpeed.Add(new KeyValuePair<DateTime, double>(
                            time, ((int)sample.WindSpeed) * Inspeed8PulseAnemom.WindSpeedMphPerHz));
                    }

                    if ((bool)config.sensors.satellite.iev2.enabled && sample.WindDirection != null)
                    {
                        sampleStore.WindDirection.Add(new KeyValuePair<DateTime, int>(
                            time, (int)sample.WindDirection));
                    }
                }
                catch
                {
                    gpio.Write(config.errorLedPin, PinValue.High);
                    Helpers.LogEvent("Failed to sample satellite sensor");
                }
            }

            if ((bool)config.sensors.rr111.enabled)
            {
                try { sampleStore.Rainfall.Add(rr111.Sample()); }
                catch
                {
                    gpio.Write(config.errorLedPin, PinValue.High);
                    Helpers.LogEvent("Failed to sample RR111 sensor");
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

                Report report = GenerateReport(time, store);
                Database.WriteReport(report, DatabaseFile.Data);

                string repstr = "{0} -- T:{1:0.0}, H:{2:0.0}, DP:{3:0.0}, WS:{4:0.0}, " +
                    "WG:{5:0.0}, WD:{6:0}, R:{7:0.000}, P:{8:0.0}";
                Console.WriteLine(string.Format(repstr, report.Time, report.AirTemperature,
                    report.RelativeHumidity, report.DewPoint, report.WindSpeed, report.WindGust,
                    report.WindDirection, report.Rainfall, report.StationPressure));

                if ((bool)config.transmitter.transmit)
                    Database.WriteReport(report, DatabaseFile.Transmit);

                // At start of new day, recalculate previous day because it needs to include the
                // data reported at 00:00:00 of the new day
                if (time.Hour == 0 && time.Minute == 0)
                {
                    DateTime local2 = TimeZoneInfo.ConvertTimeFromUtc(time - new TimeSpan(0, 1, 0),
                        config.timeZone);
                    DailyStatistic statistic2 = Database.CalculateDailyStatistic(local2, config.timeZone);
                    Database.WriteDailyStatistic(statistic2, DatabaseFile.Data);

                    if ((bool)config.transmitter.transmit)
                        Database.WriteDailyStatistic(statistic2, DatabaseFile.Transmit);
                }

                DateTime local = TimeZoneInfo.ConvertTimeFromUtc(time, config.timeZone);
                DailyStatistic statistic = Database.CalculateDailyStatistic(local, config.timeZone);
                Database.WriteDailyStatistic(statistic, DatabaseFile.Data);

                if ((bool)config.transmitter.transmit)
                    Database.WriteDailyStatistic(statistic, DatabaseFile.Transmit);

                // Keep the LED on for at least 1.5 seconds
                timer.Stop();
                if (timer.ElapsedMilliseconds < 1500)
                    Thread.Sleep(1500 - (int)timer.ElapsedMilliseconds);

                gpio.Write(config.dataLedPin, PinValue.Low);
            }).Start();
        }

        /// <summary>
        /// Produces a report from the data in a sample store and the ten minute wind stores.
        /// </summary>
        /// <param name="time">
        /// The time of the report.
        /// </param>
        /// <param name="samples">
        /// A sample store containing the data to calculate the report for.
        /// </param>
        /// <returns>
        /// The produced report.
        /// </returns>
        private Report GenerateReport(DateTime time, SampleStore samples)
        {
            Report report = new Report(time);

            if (samples.AirTemperature.Count > 0)
                report.AirTemperature = Math.Round(samples.AirTemperature.Average(), 1);

            if (samples.RelativeHumidity.Count > 0)
                report.RelativeHumidity = Math.Round(samples.RelativeHumidity.Average(), 1);

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

            if ((bool)config.sensors.rr111.enabled)
            {
                if (samples.Rainfall.Count > 0)
                    report.Rainfall = samples.Rainfall.Sum();
                else report.Rainfall = 0;
            }

            if (samples.StationPressure.Count > 0)
                report.StationPressure = Math.Round(samples.StationPressure.Average(), 1);

            if (report.AirTemperature != null && report.RelativeHumidity != null)
            {
                double dewPoint = Helpers.CalculateDewPoint(
                    (double)report.AirTemperature, (double)report.RelativeHumidity);

                report.DewPoint = Math.Round(dewPoint, 1);
            }

            if (report.StationPressure != null && report.AirTemperature != null)
            {
                double mslp = Helpers.CalculateMslp((double)report.StationPressure,
                    (double)report.AirTemperature, (double)config.position.elevation);

                report.MslPressure = Math.Round(mslp, 1);
            }

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
                    windDirection = ((int)Math.Round(Helpers.VectorAverage(vectors))) % 360;
            }

            return (windSpeed, windGust, windDirection);
        }
    }
}

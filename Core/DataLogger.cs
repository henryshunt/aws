using Aws.Hardware;
using Aws.Misc;
using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Linq;
using System.Threading;
using static Aws.Misc.Utilities2;

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
        /// The clock.
        /// </summary>
        private readonly Clock clock;

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
        /// Caches sensor samples taken between the previous and next reports (a duration of one minute).
        /// </summary>
        private SampleCache sampleCache = new SampleCache();

        /// <summary>
        /// Caches the past ten minutes of wind samples.
        /// </summary>
        private WindMonitor windMonitor = new WindMonitor();

        #region Sensors
        private Mcp9808 mcp9808 = null;
        private Bme680 bme680 = null;
        private Satellite satellite = null;
        private RainwiseRainew111 rr111 = null;
        #endregion

        /// <summary>
        /// Occurs when a new report is successfully logged to the database. Always occurs on a new thread.
        /// </summary>
        public event EventHandler<DataLoggerEventArgs> DataLogged;

        /// <summary>
        /// Initialises a new instance of the <see cref="DataLogger"/> class.
        /// </summary>
        /// <param name="config">
        /// The configuration data.
        /// </param>
        /// <param name="clock">
        /// The clock. The data logger will start operating when the clock is started.
        /// </param>
        /// <param name="gpio">
        /// The GPIO controller.
        /// </param>
        public DataLogger(Configuration config, Clock clock, GpioController gpio)
        {
            this.config = config;
            this.clock = clock;
            this.gpio = gpio;

            clock.Ticked += Clock_Ticked;
        }

        /// <summary>
        /// Opens a connection to each of the sensors enabled in the configuration.
        /// </summary>
        /// <returns>
        /// <see langword="false"/> if any of the sensors failed to open, otherwise <see langword="true"/>.
        /// </returns>
        public bool Open()
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
                    LogEvent("Failed to open MCP9808 sensor");
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
                    LogEvent("Failed to open BME680 sensor");
                    success = false;
                }
            }

            if ((bool)config.sensors.satellite.i8pa.enabled ||
                (bool)config.sensors.satellite.iev2.enabled ||
                (bool)config.sensors.satellite.isds.enabled)
            {
                SatelliteConfiguration satConfig = new SatelliteConfiguration();

                if ((bool)config.sensors.satellite.i8pa.enabled)
                {
                    satConfig.WindSpeedEnabled = true;
                    satConfig.WindSpeedPin = (int)config.sensors.satellite.i8pa.pin;
                }

                if ((bool)config.sensors.satellite.iev2.enabled)
                {
                    satConfig.WindDirectionEnabled = true;
                    satConfig.WindDirectionPin = (int)config.sensors.satellite.iev2.pin;
                }

                if ((bool)config.sensors.satellite.isds.enabled)
                {
                    satConfig.SunshineDurationEnabled = true;
                    satConfig.SunshineDurationPin = (int)config.sensors.satellite.isds.pin;
                }

                try
                {
                    satellite = new Satellite((int)config.sensors.satellite.port, satConfig);
                    satellite.Open();
                }
                catch
                {
                    gpio.Write(config.errorLedPin, PinValue.High);
                    LogEvent("Failed to open satellite sensor");
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
                    LogEvent("Failed to open RR111 sensor");
                    success = false;
                }
            }

            return success;
        }

        public void Clock_Ticked(object sender, ClockTickedEventArgs e)
        {
            if (!isSampling)
            {
                if (e.Time.Second == 0)
                {
                    startTime = e.Time;
                    isSampling = true;
                }
                else
                {
                    gpio.Write(config.dataLedPin, PinValue.High);
                    Thread.Sleep(500);
                    gpio.Write(config.dataLedPin, PinValue.Low);
                }

                return;
            }

            try { Sample(e.Time); }
            catch { }

            if (e.Time.Second == 0)
            {
                // New thread allows further sampling to continue in the background
                new Thread(() =>
                {
                    gpio.Write(config.errorLedPin, PinValue.Low);

                    try { Log(e.Time); }
                    catch (Exception ex)
                    {
                        gpio.Write(config.errorLedPin, PinValue.High);
                        LogException(ex);
                        return;
                    }

                    gpio.Write(config.dataLedPin, PinValue.High);
                    Thread.Sleep(1500);
                    gpio.Write(config.dataLedPin, PinValue.Low);

                    DataLogged?.Invoke(this, new DataLoggerEventArgs(e.Time));
                }).Start();
            }
        }

        /// <summary>
        /// Samples each of the sensors and stores the values in <see cref="sampleCache"/>.
        /// </summary>
        /// <param name="time">
        /// The current time.
        /// </param>
        private void Sample(DateTime time)
        {
            if ((bool)config.sensors.mcp9808.enabled)
                sampleCache.AirTemperature.Add(mcp9808.Sample());

            if ((bool)config.sensors.bme680.enabled)
            {
                Tuple<double, double, double> sample = bme680.Sample();
                sampleCache.RelativeHumidity.Add(sample.Item2);
                sampleCache.StationPressure.Add(sample.Item3);
            }

            if ((bool)config.sensors.satellite.enabled)
            {
                SatelliteSample sample = satellite.Sample();

                if (sample.WindSpeed != null)
                {
                    sampleCache.WindSpeed.Add(new KeyValuePair<DateTime, double>(time,
                        ((int)sample.WindSpeed) * Inspeed8PulseAnemom.WindSpeedMsPerHz));
                }

                if (sample.WindDirection != null)
                {
                    sampleCache.WindDirection.Add(new KeyValuePair<DateTime, double>(
                        time, (double)sample.WindDirection));
                }

                if (sample.SunshineDuration != null)
                    sampleCache.SunshineDuration.Add((bool)sample.SunshineDuration);
            }

            if ((bool)config.sensors.rr111.enabled)
                sampleCache.Rainfall.Add(rr111.Sample());
        }

        /// <summary>
        /// The logging routine. Produces and logs a report, and generates and logs statistics.
        /// </summary>
        /// <param name="time">
        /// The current time.
        /// </param>
        private void Log(DateTime time)
        {
            SampleCache samples = sampleCache;
            sampleCache = new SampleCache();

            windMonitor.CacheSamples(time, samples.WindSpeed, samples.WindDirection);

            Report report = GenerateReport(time, samples);
            Database.WriteReport(report, DatabaseFile.Data);

            if ((bool)config.transmitter.transmit)
                Database.WriteReport(report, DatabaseFile.Transmit);

            // At start of new day, recalculate previous day's statistics because it needs to
            // include the data reported at 00:00:00 of the new day
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
        }

        /// <summary>
        /// Produces a report from the samples in a sample cache and the wind monitor.
        /// </summary>
        /// <param name="time">
        /// The current time.
        /// </param>
        /// <param name="samples">
        /// A sample cache containing the samples to produce the report for.
        /// </param>
        /// <returns>
        /// The produced report.
        /// </returns>
        private Report GenerateReport(DateTime time, SampleCache samples)
        {
            Report report = new Report(time);

            if (samples.AirTemperature.Count > 0)
                report.AirTemperature = Math.Round(samples.AirTemperature.Average(), 1);

            if (samples.RelativeHumidity.Count > 0)
                report.RelativeHumidity = Math.Round(samples.RelativeHumidity.Average(), 1);

            // Need at least 10 minutes of wind data
            if (time >= startTime + TimeSpan.FromMinutes(10))
            {
                (double?, double?, double?) windValues = windMonitor.CalculateSummaryValues();

                if (windValues.Item1 != null)
                    report.WindSpeed = Math.Round((double)windValues.Item1, 1);
                if (windValues.Item2 != null)
                    report.WindDirection = (int)Math.Round((double)windValues.Item2, 0) % 360;
                if (windValues.Item3 != null)
                    report.WindGust = Math.Round((double)windValues.Item3, 1);
            }

            if (samples.Rainfall.Count > 0)
                report.Rainfall = samples.Rainfall.Sum();

            if (samples.SunshineDuration.Count > 0)
                report.SunshineDuration = samples.SunshineDuration.Count(s => s);

            if (samples.StationPressure.Count > 0)
                report.StationPressure = Math.Round(samples.StationPressure.Average(), 1);

            if (report.AirTemperature != null && report.RelativeHumidity != null)
            {
                double dewPoint = CalculateDewPoint((double)report.AirTemperature,
                    (double)report.RelativeHumidity);

                report.DewPoint = Math.Round(dewPoint, 1);
            }

            if (report.StationPressure != null && report.AirTemperature != null)
            {
                double mslp = CalculateMslp((double)report.StationPressure,
                    (double)report.AirTemperature, (double)config.position.elevation);

                report.MslPressure = Math.Round(mslp, 1);
            }

            return report;
        }
    }
}

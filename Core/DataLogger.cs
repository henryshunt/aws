using Aws.Hardware;
using Aws.Misc;
using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Linq;
using System.Threading;
using static Aws.Misc.Utilities;

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
        /// The time the data logger started sampling from the sensors, in UTC.
        /// </summary>
        private DateTime startTime;

        /// <summary>
        /// Caches sensor samples taken between the previous and next observation (a duration of one minute).
        /// </summary>
        private SampleCache sampleCache = new SampleCache();

        /// <summary>
        /// Caches the past ten minutes of wind samples.
        /// </summary>
        private readonly WindMonitor windMonitor = new WindMonitor();

        #region Sensors
        /// <summary>
        /// The air temperature sensor.
        /// </summary>
        private Mcp9808 airTempSensor = null;

        /// <summary>
        /// The BME680 sensor.
        /// </summary>
        private Bme680 bme680 = null;

        /// <summary>
        /// The satellite device.
        /// </summary>
        private Satellite satellite = null;

        /// <summary>
        /// The time that the wind speed sensor was last sampled successfully.
        /// </summary>
        private DateTime? lastWindSpeedSampleTime = null;

        /// <summary>
        /// The rainfall sensor.
        /// </summary>
        private RainwiseRainew111 rainfallSensor = null;

        /// <summary>
        /// The time that <see cref="rainfallSensor"/> was last sampled successfully.
        /// </summary>
        private DateTime? lastRainfallSampleTime = null;
        #endregion

        /// <summary>
        /// Occurs when data is logged to the database. Always occurs on a new thread.
        /// </summary>
        public event EventHandler<DataLoggedEventArgs> DataLogged;

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
        /// Opens each of the sensors enabled in the configuration.
        /// </summary>
        /// <returns>
        /// <see langword="false"/> if any of the sensors failed to open, otherwise <see langword="true"/>.
        /// </returns>
        public bool Open()
        {
            bool success = true;

            if (config.IsSensorEnabled(AwsSensor.AirTemperature))
            {
                try
                {
                    airTempSensor = new Mcp9808();
                    airTempSensor.Open();
                }
                catch
                {
                    gpio.Write(config.errorLedPin, PinValue.High);
                    LogMessage("Failed to open airTemp sensor");
                    success = false;
                }
            }

            if (config.IsSensorEnabled(AwsSensor.Bme680))
            {
                try
                {
                    bme680 = new Bme680();
                    bme680.Open();
                }
                catch
                {
                    gpio.Write(config.errorLedPin, PinValue.High);
                    LogMessage("Failed to open BME680 sensor");
                    success = false;
                }
            }

            if (config.IsSensorEnabled(AwsSensor.Satellite))
            {
                SatelliteConfiguration satConfig = new SatelliteConfiguration();

                if (config.IsSensorEnabled(AwsSensor.WindSpeed))
                {
                    satConfig.WindSpeedEnabled = true;
                    satConfig.WindSpeedPin = (int)config.sensors.satellite.windSpeed.pin;
                }

                if (config.IsSensorEnabled(AwsSensor.WindDirection))
                {
                    satConfig.WindDirectionEnabled = true;
                    satConfig.WindDirectionPin = (int)config.sensors.satellite.windDir.pin;
                }

                if (config.IsSensorEnabled(AwsSensor.SunshineDuration))
                {
                    satConfig.SunshineDurationEnabled = true;
                    satConfig.SunshineDurationPin = (int)config.sensors.satellite.sunDur.pin;
                }

                try
                {
                    satellite = new Satellite((int)config.sensors.satellite.port, satConfig);
                    satellite.Open();
                }
                catch
                {
                    gpio.Write(config.errorLedPin, PinValue.High);
                    LogMessage("Failed to open satellite sensor");
                    success = false;
                }
            }

            if (config.IsSensorEnabled(AwsSensor.Rainfall))
            {
                try
                {
                    rainfallSensor = new RainwiseRainew111(gpio, (int)config.sensors.rainfall.pin);
                    rainfallSensor.Open();
                }
                catch
                {
                    gpio.Write(config.errorLedPin, PinValue.High);
                    LogMessage("Failed to open rainfall sensor");
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

            Sample(e.Time);

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

                    DataLogged?.Invoke(this, new DataLoggedEventArgs(e.Time));
                }).Start();
            }
        }

        /// <summary>
        /// Samples each of the sensors and stores the values in <see cref="sampleCache"/>.
        /// </summary>
        /// <param name="time">
        /// The current time, in UTC.
        /// </param>
        private void Sample(DateTime time)
        {
            if (config.IsSensorEnabled(AwsSensor.AirTemperature))
            {
                try { sampleCache.AirTemperature.Add(airTempSensor.Sample()); }
                catch { }
            }

            if (config.IsSensorEnabled(AwsSensor.Bme680))
            {
                try
                {
                    Tuple<double, double, double> sample = bme680.Sample();
                    sampleCache.RelativeHumidity.Add(sample.Item2);
                    sampleCache.StationPressure.Add(sample.Item3);
                }
                catch { }
            }

            if (config.IsSensorEnabled(AwsSensor.Satellite))
            {
                try
                {
                    SatelliteSample sample = satellite.Sample();

                    if (sample.WindSpeed != null)
                    {
                        if (lastWindSpeedSampleTime != null &&
                            lastWindSpeedSampleTime == time - TimeSpan.FromSeconds(1))
                        {
                            sampleCache.WindSpeed.Add(new KeyValuePair<DateTime, double>(time,
                                ((int)sample.WindSpeed) * Inspeed8PulseAnemometer.WindSpeedMsPerHz));
                        }

                        lastWindSpeedSampleTime = time;
                    }

                    if (sample.WindDirection != null)
                    {
                        sampleCache.WindDirection.Add(new KeyValuePair<DateTime, double>(
                            time, (double)sample.WindDirection));
                    }

                    if (sample.SunshineDuration != null)
                        sampleCache.SunshineDuration.Add((bool)sample.SunshineDuration);
                }
                catch { }
            }

            if (config.IsSensorEnabled(AwsSensor.Rainfall))
            {
                try
                {
                    double rainfall = rainfallSensor.Sample();

                    if (lastRainfallSampleTime != null &&
                        lastRainfallSampleTime == time - TimeSpan.FromSeconds(1))
                    {
                        sampleCache.Rainfall.Add(rainfall);
                    }

                    lastRainfallSampleTime = time;
                }
                catch { }
            }
        }

        /// <summary>
        /// The logging routine. Produces and logs an observation, and generates and logs statistics.
        /// </summary>
        /// <param name="time">
        /// The current time, in UTC.
        /// </param>
        private void Log(DateTime time)
        {
            SampleCache samples = sampleCache;
            sampleCache = new SampleCache();

            windMonitor.CacheSamples(time, samples.WindSpeed, samples.WindDirection);

            Observation observation = GenerateObservation(time, samples);
            Database.WriteObservation(observation, DatabaseFile.Data);

            if ((bool)config.uploader.upload)
                Database.WriteObservation(observation, DatabaseFile.Upload);

            DateTime local = TimeZoneInfo.ConvertTimeFromUtc(time, config.position.timeZone);

            // At the start of a new day, recalculate the previous day's statistics because they
            // need to include the observation from 00:00:00 of the new day
            if (local.Hour == 0 && local.Minute == 0)
            {
                DateTime local2 = local - TimeSpan.FromMinutes(1);
                DailyStatistics statistics2 = Database.CalculateDailyStatistics(local2,
                    config.position.timeZone);

                Database.WriteDailyStatistics(statistics2, DatabaseFile.Data);
                if ((bool)config.uploader.upload)
                    Database.WriteDailyStatistics(statistics2, DatabaseFile.Upload);
            }

            DailyStatistics statistics = Database.CalculateDailyStatistics(local,
                config.position.timeZone);

            Database.WriteDailyStatistics(statistics, DatabaseFile.Data);
            if ((bool)config.uploader.upload)
                Database.WriteDailyStatistics(statistics, DatabaseFile.Upload);
        }

        /// <summary>
        /// Produces an observation from the samples in a sample cache and the wind monitor.
        /// </summary>
        /// <param name="time">
        /// The current time, in UTC.
        /// </param>
        /// <param name="samples">
        /// A sample cache containing the samples to produce the observation for.
        /// </param>
        /// <returns>
        /// The produced observation.
        /// </returns>
        private Observation GenerateObservation(DateTime time, SampleCache samples)
        {
            Observation observation = new Observation(time);

            if (samples.AirTemperature.Count > 0)
                observation.AirTemperature = Math.Round(samples.AirTemperature.Average(), 1);

            if (samples.RelativeHumidity.Count > 0)
                observation.RelativeHumidity = Math.Round(samples.RelativeHumidity.Average(), 1);

            // Need at least 10 minutes of wind data
            if (time >= startTime + TimeSpan.FromMinutes(10))
            {
                (double?, double?, double?) windValues = windMonitor.CalculateSummaryValues();

                if (windValues.Item1 != null)
                    observation.WindSpeed = Math.Round((double)windValues.Item1, 1);
                if (windValues.Item2 != null && observation.WindSpeed != null && observation.WindSpeed > 0)
                    observation.WindDirection = (int)Math.Round((double)windValues.Item2, 0) % 360;
                if (windValues.Item3 != null)
                    observation.WindGust = Math.Round((double)windValues.Item3, 1);
            }

            if (samples.Rainfall.Count > 0)
                observation.Rainfall = samples.Rainfall.Sum();

            if (samples.SunshineDuration.Count > 0)
                observation.SunshineDuration = samples.SunshineDuration.Count(s => s);

            if (samples.StationPressure.Count > 0)
                observation.StationPressure = Math.Round(samples.StationPressure.Average(), 1);

            if (observation.AirTemperature != null && observation.RelativeHumidity != null)
            {
                double dewPoint = CalculateDewPoint((double)observation.AirTemperature,
                    (double)observation.RelativeHumidity);

                observation.DewPoint = Math.Round(dewPoint, 1);
            }

            if (observation.StationPressure != null && observation.AirTemperature != null)
            {
                double mslp = CalculateMeanSeaLevelPressure((double)observation.StationPressure,
                    (double)observation.AirTemperature, (double)config.position.elevation);

                observation.MslPressure = Math.Round(mslp, 1);
            }

            return observation;
        }
    }
}

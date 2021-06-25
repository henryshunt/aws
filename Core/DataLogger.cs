using Aws.Misc;
using Aws.Sensors;
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
    internal class DataLogger : IDisposable
    {
        private readonly Configuration config;
        private readonly Clock clock;
        private readonly GpioController gpio;

        /// <summary>
        /// Indicates whether the data logger is open.
        /// </summary>
        public bool IsOpen { get; private set; } = false;

        /// <summary>
        /// Indicates whether the data logger has been started.
        /// </summary>
        public bool IsStarted { get; private set; } = false;

        /// <summary>
        /// Indicates whether the data logger has started sampling from the sensors.
        /// </summary>
        private bool isSampling = false;

        /// <summary>
        /// The time the data logger started sampling from the sensors, in UTC.
        /// </summary>
        private DateTime startTime;

        /// <summary>
        /// Buffers sensor samples taken between the previous and next observation (a duration of one minute).
        /// </summary>
        private SampleBuffer sampleBuffer = new SampleBuffer();

        /// <summary>
        /// Buffers the past ten minutes of wind samples.
        /// </summary>
        private readonly WindMonitor windMonitor = new WindMonitor();

        /// <summary>
        /// Stores the thread created in <see cref="Clock_Ticked(object, ClockTickedEventArgs)"/> so it can be joined
        /// in <see cref="Dispose"/>.
        /// </summary>
        private Thread loggingThread = null;

        #region Sensors
        private Mcp9808 airTempSensor = null;
        private Htu21d relHumSensor = null;
        private Satellite satellite = null;

        /// <summary>
        /// The time that the wind speed sensor was last sampled successfully.
        /// </summary>
        private DateTime? lastWindSpeedSampleTime = null;

        private RainwiseRainew111 rainfallSensor = null;

        /// <summary>
        /// The time that <see cref="rainfallSensor"/> was last sampled successfully.
        /// </summary>
        private DateTime? lastRainfallSampleTime = null;

        private Bmp280 staPresSensor = null;
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
        /// The clock.
        /// </param>
        /// <param name="gpio">
        /// The GPIO controller.
        /// </param>
        public DataLogger(Configuration config, Clock clock, GpioController gpio)
        {
            this.config = config;
            this.clock = clock;
            this.gpio = gpio;
        }

        /// <summary>
        /// Opens the data logger.
        /// </summary>
        /// <returns>
        /// <see langword="false"/> if any of the sensors failed to open, otherwise <see langword="true"/>.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the data logger is already open.
        /// </exception>
        public bool Open()
        {
            if (IsOpen)
                throw new InvalidOperationException("The data logger is already open");
            IsOpen = true;

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

            if (config.IsSensorEnabled(AwsSensor.RelativeHumidity))
            {
                try
                {
                    relHumSensor = new Htu21d();
                    relHumSensor.Open();
                }
                catch
                {
                    gpio.Write(config.errorLedPin, PinValue.High);
                    LogMessage("Failed to open relHum sensor");
                    success = false;
                }
            }

            if (config.IsSensorEnabled(AwsSensor.StationPressure))
            {
                try
                {
                    staPresSensor = new Bmp280();
                    staPresSensor.Open();
                }
                catch
                {
                    gpio.Write(config.errorLedPin, PinValue.High);
                    LogMessage("Failed to open BMP280 sensor");
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
                    rainfallSensor = new RainwiseRainew111(
                        (int)config.sensors.rainfall.pin, gpio);

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

        /// <summary>
        /// Closes the data logger.
        /// </summary>
        public void Close()
        {
            clock.Ticked -= Clock_Ticked;

            airTempSensor?.Dispose();
            relHumSensor?.Dispose();
            staPresSensor?.Dispose();
            satellite?.Dispose();
            rainfallSensor?.Dispose();

            if (loggingThread != null)
                loggingThread?.Join();

            IsOpen = false;
        }

        /// <summary>
        /// Starts the data logger.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the data logger is not open or has already been started.
        /// </exception>
        public void Start()
        {
            if (!IsOpen)
                throw new InvalidOperationException("The data logger is not open");
            else if (IsStarted)
                throw new InvalidOperationException("The data logger has already been started");

            clock.Ticked += Clock_Ticked;
        }

        /// <exception cref="InvalidOperationException">
        /// Thrown if the data logger is not open.
        /// </exception>
        private void Clock_Ticked(object sender, ClockTickedEventArgs e)
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
                loggingThread = new Thread(TopOfMinute);
                loggingThread.Start(e.Time);
            }
        }

        /// <summary>
        /// Samples each of the sensors and stores the values in <see cref="sampleBuffer"/>.
        /// </summary>
        /// <param name="time">
        /// The current time, in UTC.
        /// </param>
        private void Sample(DateTime time)
        {
            List<Thread> threads = new List<Thread>();

            threads.Add(new Thread(() =>
            {
                if (config.IsSensorEnabled(AwsSensor.AirTemperature))
                {
                    try
                    {
                        sampleBuffer.AirTemperature.Add(airTempSensor.Sample());
                    }
                    catch { gpio.Write(config.errorLedPin, PinValue.High); }
                }

                if (config.IsSensorEnabled(AwsSensor.RelativeHumidity))
                {
                    try
                    {
                        sampleBuffer.RelativeHumidity.Add(relHumSensor.Sample());
                    }
                    catch { gpio.Write(config.errorLedPin, PinValue.High); }
                }

                if (config.IsSensorEnabled(AwsSensor.StationPressure))
                {
                    try
                    {
                        sampleBuffer.StationPressure.Add(staPresSensor.Sample());
                    }
                    catch { gpio.Write(config.errorLedPin, PinValue.High); }
                }
            }));

            threads.Add(new Thread(() =>
            {
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
                                sampleBuffer.WindSpeed.Add(new KeyValuePair<DateTime, double>(time,
                                    ((int)sample.WindSpeed) * Inspeed8PulseAnemometer.MS_PER_HZ));
                            }

                            lastWindSpeedSampleTime = time;
                        }

                        if (sample.WindDirection != null)
                        {
                            sampleBuffer.WindDirection.Add(new KeyValuePair<DateTime, double>(
                                time, (double)sample.WindDirection));
                        }

                        if (sample.SunshineDuration != null)
                            sampleBuffer.SunshineDuration.Add((bool)sample.SunshineDuration);
                    }
                    catch
                    {
                        gpio.Write(config.errorLedPin, PinValue.High);
                    }
                }
            }));

            threads.Add(new Thread(() =>
            {
                if (config.IsSensorEnabled(AwsSensor.Rainfall))
                {
                    try
                    {
                        double rainfall = rainfallSensor.Sample();

                        if (lastRainfallSampleTime != null &&
                            lastRainfallSampleTime == time - TimeSpan.FromSeconds(1))
                        {
                            sampleBuffer.Rainfall.Add(rainfall);
                        }

                        lastRainfallSampleTime = time;
                    }
                    catch
                    {
                        gpio.Write(config.errorLedPin, PinValue.High);
                    }
                }
            }));

            foreach (Thread thread in threads)
                thread.Start();

            foreach (Thread thread in threads)
                thread.Join();
        }

        /// <summary>
        /// The method that is called at the top of every minute to deal with the past minute of new samples. Logs an
        /// observation, writes statistics and invokes <see cref="DataLogged"/>, among other things.
        /// </summary>
        /// <param name="time">
        /// A <see cref="DateTime"/> containing the current time, in UTC. This is an <see cref="object"/> because the
        /// method is designed to be used with <see cref="Thread.Start(object?)"/>.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="time"/> is not of type <see cref="DateTime"/>.
        /// </exception>
        private void TopOfMinute(object time)
        {
            if (time.GetType() != typeof(DateTime))
            {
                throw new ArgumentException(
                    nameof(time) + " must be of type " + nameof(DateTime), nameof(time));
            }

            gpio.Write(config.errorLedPin, PinValue.Low);

            try
            {
                Log((DateTime)time);
            }
            catch (Exception ex)
            {
                gpio.Write(config.errorLedPin, PinValue.High);
                LogException(ex);
                return;
            }

            try
            {
                WriteStatistics((DateTime)time);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }

            gpio.Write(config.dataLedPin, PinValue.High);
            Thread.Sleep(1500);
            gpio.Write(config.dataLedPin, PinValue.Low);

            DataLogged?.Invoke(this, new DataLoggedEventArgs((DateTime)time));
        }

        /// <summary>
        /// Logs an observation for the samples in <see cref="sampleBuffer"/>, and empties <see cref="sampleBuffer"/>.
        /// </summary>
        /// <param name="time">
        /// The current time, in UTC.
        /// </param>
        private void Log(DateTime time)
        {
            SampleBuffer samples = sampleBuffer;
            sampleBuffer = new SampleBuffer();

            windMonitor.BufferSamples(time, samples.WindSpeed, samples.WindDirection);

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
                {
                    observation.WindDirection = ((int)Math.Round((double)windValues.Item2, 0) +
                        (int)config.sensors.satellite.windDir.offset) % 360;
                }
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

            Database.WriteObservation(observation, DatabaseFile.Data);

            if ((bool)config.uploader.upload)
                Database.WriteObservation(observation, DatabaseFile.Upload);
        }

        /// <summary>
        /// Calculates and writes various statistics over various time periods.
        /// </summary>
        /// <param name="time">
        /// The current time, in UTC.
        /// </param>
        private void WriteStatistics(DateTime time)
        {
            DateTime local = TimeZoneInfo.ConvertTimeFromUtc(time, config.position.timeZone);

            // At the start of a new day, recalculate the previous day's statistics because they
            // need to include the observation from 00:00:00 of the new day
            if (local.Hour == 0 && local.Minute == 0)
            {
                DateTime local2 = local - TimeSpan.FromMinutes(1);
                DailyStatistics statistics1 = Database.CalculateDailyStatistics(local2,
                    config.position.timeZone);

                Database.WriteDailyStatistics(statistics1, DatabaseFile.Data);
                if ((bool)config.uploader.upload)
                    Database.WriteDailyStatistics(statistics1, DatabaseFile.Upload);
            }

            DailyStatistics statistics2 = Database.CalculateDailyStatistics(local,
                config.position.timeZone);

            Database.WriteDailyStatistics(statistics2, DatabaseFile.Data);
            if ((bool)config.uploader.upload)
                Database.WriteDailyStatistics(statistics2, DatabaseFile.Upload);


            // At the start of a new month, recalculate the previous month's statistics because
            // they need to include the observation from 00:00:00 of the new month's first day
            if (local.Day == 1 && local.Hour == 0 && local.Minute == 0)
            {
                DateTime local2 = local - TimeSpan.FromMinutes(1);
                MonthlyStatistics statistics1 = Database.CalculateMonthlyStatistics(local2.Year,
                    local2.Month, config.position.timeZone);

                Database.WriteMonthlyStatistics(statistics1, DatabaseFile.Data);
                if ((bool)config.uploader.upload)
                    Database.WriteMonthlyStatistics(statistics1, DatabaseFile.Upload);
            }

            MonthlyStatistics statistics3 = Database.CalculateMonthlyStatistics(local.Year,
                    local.Month, config.position.timeZone);

            Database.WriteMonthlyStatistics(statistics3, DatabaseFile.Data);
            if ((bool)config.uploader.upload)
                Database.WriteMonthlyStatistics(statistics3, DatabaseFile.Upload);
        }

        public void Dispose() => Close();
    }
}

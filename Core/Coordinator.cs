using AWS.Routines;
using System;
using System.Device.Gpio;
using System.Diagnostics;
using System.IO;
using System.Threading;
using static AWS.Routines.Helpers;

namespace AWS.Core
{
    internal class Coordinator
    {
        private Configuration configuration;
        private Clock clock;
        private Sampler sampler;

        private DateTime StartupTime;
        private GpioController GPIO;
        private bool ShouldSkipSample = true;


        public void Startup()
        {
            LogEvent("Startup", "Began startup procedure");

            // Load configuration
            try
            {
                configuration = Configuration.Load(CONFIG_FILE);

                if (!Configuration.Validate(configuration))
                {
                    LogEvent("Startup", "Error while validating configuration file");
                    return;
                }
                else LogEvent("Startup", "Loaded configuration file");
            }
            catch (Exception ex)
            {
                LogEvent("Startup", "Error while loading configuration file", ex);
                return;
            }

            GPIO = new GpioController(PinNumberingScheme.Logical);
            GPIO.OpenPin(configuration.DataLedPin, PinMode.Output);
            GPIO.OpenPin(configuration.ErrorLedPin, PinMode.Output);
            GPIO.OpenPin(configuration.PowerLedPin, PinMode.Output);

            GPIO.Write(configuration.DataLedPin, PinValue.High);
            GPIO.Write(configuration.ErrorLedPin, PinValue.High);
            GPIO.Write(configuration.PowerLedPin, PinValue.High);
            Thread.Sleep(2500);
            GPIO.Write(configuration.DataLedPin, PinValue.Low);
            GPIO.Write(configuration.ErrorLedPin, PinValue.Low);
            GPIO.Write(configuration.PowerLedPin, PinValue.Low);

            // Initialise clock
            try
            {
                clock = new Clock(configuration.ClockTickPin);
                clock.Ticked += Clock_Ticked;

                LogEvent(clock.DateTime, "Startup", "Initialised clock");
            }
            catch
            {
                LogEvent("Startup", "Error while initialising clock");
                GPIO.Write(configuration.ErrorLedPin, PinValue.High);
                return;
            }

            StartupTime = clock.DateTime;

            // Data directory
            try { Directory.CreateDirectory(DATA_DIRECTORY); }
            catch
            {
                LogEvent(clock.DateTime, "Startup", "Error while creating data directory");
                GPIO.Write(configuration.ErrorLedPin, PinValue.High);
                return;
            }

            // Data database
            try
            {
                if (!Database.Exists(Database.DatabaseFile.Data))
                {
                    Database.Create(Database.DatabaseFile.Data);
                    LogEvent(clock.DateTime, "Startup", "Created data database");
                }
            }
            catch
            {
                LogEvent(clock.DateTime, "Startup", "Error while creating data database");
                GPIO.Write(configuration.ErrorLedPin, PinValue.High);
                return;
            }

            // Transmit database
            try
            {
                if (configuration.Transmitter.TransmitReports &&
                    !Database.Exists(Database.DatabaseFile.Transmit))
                {
                    Database.Create(Database.DatabaseFile.Transmit);
                    LogEvent(clock.DateTime, "Startup", "Created transmit database");
                }
            }
            catch
            {
                LogEvent(clock.DateTime, "Startup", "Error while creating transmit database");
                GPIO.Write(configuration.ErrorLedPin, PinValue.High);
                return;
            }

            // Initialise sensors
            try
            {
                sampler = new Sampler(configuration);
                sampler.InitialiseSensors();
            }
            catch { return; }

            GPIO.Write(configuration.DataLedPin, PinValue.High);
            GPIO.Write(configuration.ErrorLedPin, PinValue.High);
            GPIO.Write(configuration.PowerLedPin, PinValue.High);
            Thread.Sleep(1000);
            GPIO.Write(configuration.DataLedPin, PinValue.Low);
            GPIO.Write(configuration.ErrorLedPin, PinValue.Low);
            GPIO.Write(configuration.PowerLedPin, PinValue.Low);

            clock.Start();
            LogEvent(clock.DateTime, "Startup", "Started scheduling clock");

            Console.ReadKey();
        }

        private void Clock_Ticked(object sender, ClockTickedEventArgs e)
        {
            bool shouldSkipLog = false;
            bool isFirstSample = false;

            // Start sampling at the top of the first minute
            if (ShouldSkipSample && e.TickTime.Second == 0)
            {
                ShouldSkipSample = false;
                shouldSkipLog = true;
                isFirstSample = true;

                //foreach (KeyValuePair<int, Satellite> satellite in Satellites)
                //    satellite.Value.StartSensors();

                //WindSpeedSensor.IsPaused = false;
                //RainfallSensor.IsPaused = false;
            }

            if (ShouldSkipSample) return;
            SampleSensors(e.TickTime, isFirstSample);

            // Run at the top of all minutes except the first
            if (e.TickTime.Second == 0 && !shouldSkipLog)
            {
                new Thread(() =>
                {
                    LogReport(e.TickTime);
                    TransmitReports(e.TickTime);
                }).Start();
            }
        }

        private void SampleSensors(DateTime time, bool isFirstSample)
        {
            //List<Thread> satelliteThreads = new List<Thread>();
            //foreach (Satellite satellite in Satellites.Values)
            //    satelliteThreads.Add(satellite.SampleSensors());

            //if (time.Second == 0)
            //{
            //    //WindSpeedSensor.WindSpeedStore.SwapValueBuckets();
            //    RainfallSensor.RainfallStore.SwapValueBucket();

            //    BME680Sensor.TemperatureStore.SwapValueBucket();
            //    BME680Sensor.RelativeHumidityStore.SwapValueBucket();
            //    BME680Sensor.PressureStore.SwapValueBucket();
            //}

            //Thread BME680Thread = BME680Sensor.SampleSensor();

            //// Don't continue until all sensors have completed sampling
            //BME680Thread.Join();

            //foreach (Thread thread in satelliteThreads)
            //    thread.Join();

            //foreach (Satellite satellite in Satellites.Values)
            //{
            //    WindSpeedSensor.WindSpeedStore.ActiveValueBucket.Add(
            //        new KeyValuePair<DateTime, int>(time, (int)satellite.LatestSample.WindSpeed));

            //    Console.WriteLine(string.Format(
            //        "Wind Speed: {0}, Wind Direction: {1}", satellite.LatestSample.WindSpeed,
            //        satellite.LatestSample.WindDirection));
            //}
        }
        private void LogReport(DateTime time)
        {
            Stopwatch ledStopwatch = new Stopwatch();
            ledStopwatch.Start();

            GPIO.Write(configuration.DataLedPin, PinValue.High);

            Database.WriteReport(sampler.GenerateReport(time));

            // Ensure the data LED stays on for at least 1.5 seconds
            ledStopwatch.Stop();
            if (ledStopwatch.ElapsedMilliseconds < 1500)
                Thread.Sleep(1500 - (int)ledStopwatch.ElapsedMilliseconds);

            GPIO.Write(configuration.DataLedPin, PinValue.Low);
        }
        private void TransmitReports(DateTime time)
        {

        }
    }
}
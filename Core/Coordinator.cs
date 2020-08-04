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

        private DateTime startupTime;
        private GpioController gpio;
        private bool shouldSkipSample = true;


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

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            gpio = new GpioController(PinNumberingScheme.Logical);
            gpio.OpenPin(configuration.DataLedPin, PinMode.Output);
            gpio.OpenPin(configuration.ErrorLedPin, PinMode.Output);
            gpio.OpenPin(configuration.PowerLedPin, PinMode.Output);

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
                gpio.Write(configuration.ErrorLedPin, PinValue.High);
                return;
            }

            startupTime = clock.DateTime;

            // Data directory
            try { Directory.CreateDirectory(DATA_DIRECTORY); }
            catch
            {
                LogEvent(clock.DateTime, "Startup", "Error while creating data directory");
                gpio.Write(configuration.ErrorLedPin, PinValue.High);
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
                gpio.Write(configuration.ErrorLedPin, PinValue.High);
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
                gpio.Write(configuration.ErrorLedPin, PinValue.High);
                return;
            }

            // Initialise sensors
            //try
            //{
            sampler = new Sampler(configuration);
            sampler.InitialiseSensors();
            //}
            //catch (Exception ex)
            //{
            //    LogEvent(clock.DateTime, "Startup", "Error", ex);
            //    return;
            //}

            gpio.Write(configuration.DataLedPin, PinValue.High);
            gpio.Write(configuration.ErrorLedPin, PinValue.High);
            gpio.Write(configuration.PowerLedPin, PinValue.High);
            Thread.Sleep(2500);
            gpio.Write(configuration.DataLedPin, PinValue.Low);
            gpio.Write(configuration.ErrorLedPin, PinValue.Low);
            gpio.Write(configuration.PowerLedPin, PinValue.Low);

            clock.Start();
            LogEvent(clock.DateTime, "Startup", "Started scheduling clock");

            Console.ReadKey();
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LogEvent(clock.DateTime, "Global", "Unhandled exception");
        }

        private void Clock_Ticked(object sender, ClockTickedEventArgs e)
        {
            bool shouldSkipLog = false;
            bool isFirstSample = false;

            // Start sampling at the start of the next minute
            if (shouldSkipSample && e.Time.Second == 0)
            {
                shouldSkipSample = false;
                shouldSkipLog = true;
                isFirstSample = true;

                sampler.StartSensors(e.Time);
            }

            if (shouldSkipSample)
                return;

            sampler.SampleSensors(e.Time, isFirstSample);

            // Run at the start of all minutes except the first
            if (e.Time.Second == 0 && !shouldSkipLog)
            {
                new Thread(() =>
                {
                    LogReport(e.Time);
                    // Transmitter.Transmit();
                }).Start();
            }
        }

        private void LogReport(DateTime time)
        {
            Stopwatch ledStopwatch = new Stopwatch();
            ledStopwatch.Start();

            gpio.Write(configuration.DataLedPin, PinValue.High);

            Report report = sampler.GenerateReport(time);
            Database.WriteReport(report);

            // Ensure the data LED stays on for at least 1.5 seconds
            ledStopwatch.Stop();
            if (ledStopwatch.ElapsedMilliseconds < 1500)
                Thread.Sleep(1500 - (int)ledStopwatch.ElapsedMilliseconds);

            gpio.Write(configuration.DataLedPin, PinValue.Low);
        }
    }
}
using Aws.Routines;
using System;
using System.Device.Gpio;
using System.IO;
using System.Threading;

namespace Aws.Core
{
    /// <summary>
    /// Represents the base of the AWS software. Responsible for managing, controlling and
    /// coordinating everything.
    /// </summary>
    internal class Controller
    {
        /// <summary>
        /// The AWS' configuration data.
        /// </summary>
        private Configuration config;

        /// <summary>
        /// The clock used for timekeeping and operation triggering.
        /// </summary>
        private Clock clock;

        /// <summary>
        /// The data logger.
        /// </summary>
        private DataLogger dataLogger;

        /// <summary>
        /// The GPIO controller.
        /// </summary>
        private GpioController gpio;

        /// <summary>
        /// Effectively the start button for the AWS software. Initialises everything and begins
        /// AWS operation.
        /// </summary>
        public void Startup()
        {
            Helpers.LogEvent(null, nameof(Controller), "Startup");

            // Load configuration
            try
            {
                config = new Configuration(Helpers.CONFIG_FILE);

                if (!config.Load())
                {
                    Helpers.LogEvent(null, nameof(Controller),
                        "Failed to load configuration file (invalid data)");
                    return;
                }

                Helpers.LogEvent(null, nameof(Controller), "Loaded configuration file");
            }
            catch
            {
                Helpers.LogEvent(null, nameof(Controller), "Failed to load configuration file");
                return;
            }

            // Initialise GPIO
            gpio = new GpioController(PinNumberingScheme.Logical);
            gpio.OpenPin(config.dataLedPin, PinMode.Output);
            gpio.Write(config.dataLedPin, PinValue.Low);
            gpio.OpenPin(config.errorLedPin, PinMode.Output);
            gpio.Write(config.errorLedPin, PinValue.Low);

            Thread.Sleep(1000);

            // Connect to clock
            try
            {
                clock = new Clock(config.clockTickPin, gpio);
                clock.Ticked += Clock_Ticked;
                clock.Open();

                //if (!clock.IsDateTimeValid)
                //{
                //    gpio.Write(config.errorLedPin, PinValue.High);
                //    Helpers.LogEvent(null, nameof(Controller), "Clock time is invalid");
                //    return;
                //}

                Helpers.LogEvent(clock.DateTime, nameof(Controller), "Opened connection to clock");
            }
            catch
            {
                gpio.Write(config.errorLedPin, PinValue.High);
                Helpers.LogEvent(null, nameof(Controller), "Failed to open connection to clock");
                return;
            }

            if (!StartupFileSystem())
                return;

            // Connect to sensors
            try
            {
                dataLogger = new DataLogger(config, gpio);
                dataLogger.Open();
            }
            catch (DataLoggerException)
            {
                gpio.Write(config.errorLedPin, PinValue.High);
                return;
            }

            // Indicate successful startup
            gpio.Write(config.dataLedPin, PinValue.High);
            gpio.Write(config.errorLedPin, PinValue.High);
            Thread.Sleep(2500);
            gpio.Write(config.dataLedPin, PinValue.Low);
            gpio.Write(config.errorLedPin, PinValue.Low);

            // Start the clock
            try
            {
                clock.Start();
                Helpers.LogEvent(clock.DateTime, nameof(Controller), "Started clock");
            }
            catch
            {
                gpio.Write(config.errorLedPin, PinValue.High);
                Helpers.LogEvent(clock.DateTime, nameof(Controller), "Failed to start clock");
                return;
            }

            Console.ReadKey();
        }

        /// <summary>
        /// Performs various filesystem-related startup checks.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if the checks were successful, otherwise <see langword="false"/>
        /// </returns>
        private bool StartupFileSystem()
        {
            // Create data directory
            try
            {
                if (!Directory.Exists(Helpers.DATA_DIRECTORY))
                {
                    Directory.CreateDirectory(Helpers.DATA_DIRECTORY);
                    Helpers.LogEvent(clock.DateTime, nameof(Controller), "Created data directory");
                }
            }
            catch
            {
                gpio.Write(config.errorLedPin, PinValue.High);
                Helpers.LogEvent(clock.DateTime, nameof(Controller), "Failed to create data directory");
                return false;
            }

            // Create data database
            try
            {
                if (!Database.Exists(Database.DatabaseFile.Data))
                {
                    Database.Create(Database.DatabaseFile.Data);
                    Helpers.LogEvent(clock.DateTime, nameof(Controller), "Created data database");
                }
            }
            catch
            {
                gpio.Write(config.errorLedPin, PinValue.High);
                Helpers.LogEvent(clock.DateTime, nameof(Controller), "Failed to create data database");
                return false;
            }

            // Create transmit database
            //try
            //{
            //    if (config.transmitter.transmitReports && 
            //        !Database.Exists(Database.DatabaseFile.Transmit))
            //    {
            //        Database.Create(Database.DatabaseFile.Transmit);
            //        Helpers.LogEvent(clock.DateTime, "Coordinator", "Created transmit database");
            //    }
            //}
            //catch
            //{
            //    gpio.Write(config.errorLedPin, PinValue.High);
            //    Helpers.LogEvent(clock.DateTime, "Coordinator", "Failed to create transmit database");
            //    return false;
            //}

            return true;
        }

        private void Clock_Ticked(object sender, ClockTickedEventArgs e)
        {
            try
            {
                dataLogger.Tick(e.Time);
            }
            catch (DataLoggerException ex)
            when (ex.Message == "Failed to start sampling")
            {
                gpio.Write(config.errorLedPin, PinValue.High);
                clock.Stop();
            }
        }
    }
}
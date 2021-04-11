using Aws.Routines;
using System;
using System.Device.Gpio;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

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
        private Configuration config = new Configuration();

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
        public async Task Startup()
        {
            Helpers.LogEvent(nameof(Controller), "Startup ------------");

            // Load configuration
            try { await config.LoadAsync(); }
            catch (Exception ex)
            {
                Helpers.LogException(ex);
                return;
            }

            // Initialise GPIO
            gpio = new GpioController(PinNumberingScheme.Logical);
            gpio.OpenPin(config.dataLedPin, PinMode.Output);
            gpio.Write(config.dataLedPin, PinValue.Low);
            gpio.OpenPin(config.errorLedPin, PinMode.Output);
            gpio.Write(config.errorLedPin, PinValue.Low);

            Thread.Sleep(1000);

            // Open clock
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

                Helpers.LogEvent(nameof(Controller), "Opened connection to clock");
                Helpers.LogEvent(nameof(Controller), "Current clock time is " +
                    clock.DateTime.ToString("yyyy-MM-dd HH:mm:ss"));
            }
            catch
            {
                gpio.Write(config.errorLedPin, PinValue.High);
                Helpers.LogEvent(nameof(Controller), "Failed to open connection to clock");
                return;
            }

            // Filesystem related work
            if (!StartupFileSystem())
                return;

            // Open data logger
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

            gpio.Write(config.dataLedPin, PinValue.High);
            gpio.Write(config.errorLedPin, PinValue.High);
            Thread.Sleep(2500);
            gpio.Write(config.dataLedPin, PinValue.Low);
            gpio.Write(config.errorLedPin, PinValue.Low);

            // Start clock
            try
            {
                clock.Start();
                Helpers.LogEvent(nameof(Controller), "Started clock");
            }
            catch
            {
                gpio.Write(config.errorLedPin, PinValue.High);
                Helpers.LogEvent(nameof(Controller), "Failed to start clock");
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
                    Helpers.LogEvent(nameof(Controller), "Created data directory");
                }
            }
            catch
            {
                gpio.Write(config.errorLedPin, PinValue.High);
                Helpers.LogEvent(nameof(Controller), "Failed to create data directory");
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
                Helpers.LogEvent(nameof(Controller), "Failed to create data database");
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
            //gpio.Write(config.errorLedPin, PinValue.High);
            //    Helpers.LogEvent("Coordinator", "Failed to create transmit database");
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
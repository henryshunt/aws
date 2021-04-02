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
        private Configuration configuration = new Configuration();

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
        public async void Startup()
        {
            Helpers.LogEvent(null, nameof(Controller), "Startup");

            if (!await configuration.LoadAsync(Helpers.CONFIG_FILE))
                return;

            gpio = new GpioController(PinNumberingScheme.Logical);
            gpio.OpenPin(configuration.dataLedPin, PinMode.Output);
            gpio.Write(configuration.dataLedPin, PinValue.Low);
            gpio.OpenPin(configuration.errorLedPin, PinMode.Output);
            gpio.Write(configuration.errorLedPin, PinValue.Low);

            Thread.Sleep(1000);


            try
            {
                clock = new Clock(configuration.clockTickPin, gpio);
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
                gpio.Write(configuration.errorLedPin, PinValue.High);
                Helpers.LogEvent(null, nameof(Controller), "Failed to open connection to clock");
                return;
            }

            if (!StartupFileSystem())
            {
                gpio.Write(configuration.errorLedPin, PinValue.High);
                return;
            }

            try
            {
                if (!File.Exists(Helpers.GPS_FILE))
                {
                    // Do GPS acquisition
                }
            }
            catch
            {
                Helpers.LogEvent(null, nameof(Controller), "Failed to acquire GPS data");
                gpio.Write(configuration.errorLedPin, PinValue.High);
                return;
            }

            if (!configuration.LoadGps(Helpers.GPS_FILE))
            {
                gpio.Write(configuration.errorLedPin, PinValue.High);
                return;
            }


            try
            {
                dataLogger = new DataLogger(configuration, gpio);
                dataLogger.Open();
            }
            catch (DataLoggerException)
            {
                gpio.Write(configuration.errorLedPin, PinValue.High);
                return;
            }

            gpio.Write(configuration.dataLedPin, PinValue.High);
            gpio.Write(configuration.errorLedPin, PinValue.High);
            Thread.Sleep(2500);
            gpio.Write(configuration.dataLedPin, PinValue.Low);
            gpio.Write(configuration.errorLedPin, PinValue.Low);

            try
            {
                clock.Start();
                Helpers.LogEvent(clock.DateTime, nameof(Controller), "Started clock");
            }
            catch
            {
                gpio.Write(configuration.errorLedPin, PinValue.High);
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
                gpio.Write(configuration.errorLedPin, PinValue.High);
                clock.Stop();
            }
        }
    }
}
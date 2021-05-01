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
            Helpers.LogEvent("Startup ------------");

            try { await config.LoadAsync(); }
            catch (Exception ex)
            {
                Helpers.LogException(ex);
                return;
            }

            gpio = new GpioController(PinNumberingScheme.Logical);
            gpio.OpenPin(config.dataLedPin, PinMode.Output);
            gpio.Write(config.dataLedPin, PinValue.Low);
            gpio.OpenPin(config.errorLedPin, PinMode.Output);
            gpio.Write(config.errorLedPin, PinValue.Low);

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

                Helpers.LogEvent("Clock time is " +
                    clock.DateTime.ToString("yyyy-MM-dd HH:mm:ss"));
            }
            catch (Exception ex)
            {
                gpio.Write(config.errorLedPin, PinValue.High);
                Helpers.LogException(ex);
                return;
            }

            if (!StartupFileSystem())
                return;

            dataLogger = new DataLogger(config, gpio);
            dataLogger.StartFailed += DataLogger_StartFailed;
            dataLogger.DataLogged += DataLogger_DataLogged;
            dataLogger.Open();

            gpio.Write(config.dataLedPin, PinValue.High);
            gpio.Write(config.errorLedPin, PinValue.High);
            Thread.Sleep(2500);
            gpio.Write(config.dataLedPin, PinValue.Low);
            gpio.Write(config.errorLedPin, PinValue.Low);

            try { clock.Start(); }
            catch
            {
                gpio.Write(config.errorLedPin, PinValue.High);
                Helpers.LogEvent("Failed to start clock");
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
            try
            {
                if (!Directory.Exists(Helpers.DATA_DIRECTORY))
                    Directory.CreateDirectory(Helpers.DATA_DIRECTORY);
            }
            catch
            {
                gpio.Write(config.errorLedPin, PinValue.High);
                Helpers.LogEvent("Failed to create data directory");
                return false;
            }

            try
            {
                if (!Database.Exists(DatabaseFile.Data))
                    Database.Create(DatabaseFile.Data);
            }
            catch
            {
                gpio.Write(config.errorLedPin, PinValue.High);
                Helpers.LogEvent("Failed to create data database");
                return false;
            }

            try
            {
                if ((bool)config.transmitter.transmit &&
                    !Database.Exists(DatabaseFile.Transmit))
                {
                    Database.Create(DatabaseFile.Transmit);
                }
            }
            catch
            {
                gpio.Write(config.errorLedPin, PinValue.High);
                Helpers.LogEvent("Failed to create transmit database");
                return false;
            }

            return true;
        }

        private void Clock_Ticked(object sender, ClockTickedEventArgs e)
        {
            dataLogger.Tick(e.Time);
        }

        private void DataLogger_StartFailed(object sender, EventArgs e)
        {
            gpio.Write(config.errorLedPin, PinValue.High);
            clock.Stop();
        }

        private void DataLogger_DataLogged(object sender, DataLoggerEventArgs e)
        {
            // Transmit data
        }
    }
}
using Aws.Misc;
using System;
using System.Device.Gpio;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using static Aws.Misc.Utilities;

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
        private readonly Configuration config = new Configuration();

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
            LogMessage("Startup ------------");

            try { await config.LoadAsync(); }
            catch (Exception ex)
            {
                LogException(ex);
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
                clock.Open();

                LogMessage("Clock time is " +
                    clock.DateTime.ToString("yyyy-MM-dd HH:mm:ss"));
            }
            catch (Exception ex)
            {
                gpio.Write(config.errorLedPin, PinValue.High);
                LogException(ex);
                return;
            }

            if (!StartupFileSystem())
                return;

            dataLogger = new DataLogger(config, clock, gpio);
            dataLogger.DataLogged += DataLogger_DataLogged;

            if (!dataLogger.Open())
            {
                for (int i = 0; i < 5; i++)
                {
                    gpio.Write(config.errorLedPin, PinValue.High);
                    Thread.Sleep(250);
                    gpio.Write(config.errorLedPin, PinValue.Low);
                    Thread.Sleep(250);
                }
            }

            Thread.Sleep(1500);
            gpio.Write(config.dataLedPin, PinValue.High);
            gpio.Write(config.errorLedPin, PinValue.High);
            Thread.Sleep(2500);
            gpio.Write(config.dataLedPin, PinValue.Low);
            gpio.Write(config.errorLedPin, PinValue.Low);
            Thread.Sleep(1500);

            try
            {
                clock.StartTickEvents();
            }
            catch
            {
                gpio.Write(config.errorLedPin, PinValue.High);
                LogMessage("Failed to start clock");
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
                if (!Directory.Exists(DATA_DIRECTORY))
                    Directory.CreateDirectory(DATA_DIRECTORY);
            }
            catch
            {
                gpio.Write(config.errorLedPin, PinValue.High);
                LogMessage("Failed to create data directory");
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
                LogMessage("Failed to create data database");
                return false;
            }

            try
            {
                if ((bool)config.uploader.upload &&
                    !Database.Exists(DatabaseFile.Upload))
                {
                    Database.Create(DatabaseFile.Upload);
                }
            }
            catch
            {
                gpio.Write(config.errorLedPin, PinValue.High);
                LogMessage("Failed to create upload database");
                return false;
            }

            return true;
        }

        private void DataLogger_DataLogged(object sender, DataLoggedEventArgs e)
        {
            // Upload data
        }
    }
}
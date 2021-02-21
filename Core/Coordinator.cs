using Aws.Routines;
using NLog;
using System;
using System.Device.Gpio;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Aws.Core
{
    internal class Coordinator
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private Configuration config;
        private Clock clock;
        private Sampler sampler;

        private GpioController gpio;
        private DateTime startupTime;
        private bool isSampling = false;


        public void Startup()
        {
            logger.Info("Began AWS startup procedure");

            // Load configuration
            try
            {
                config = new Configuration(Helpers.CONFIG_FILE);

                if (!config.Load())
                {
                    logger.Error("Invalid configuration file");
                    return;
                }

                logger.Info("Loaded configuration file");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error while loading configuration file");
                return;
            }

            // Initialise LED indicators
            gpio = new GpioController(PinNumberingScheme.Logical);

            gpio.OpenPin(config.dataLedPin, PinMode.Output);
            gpio.Write(config.dataLedPin, PinValue.Low);
            gpio.OpenPin(config.errorLedPin, PinMode.Output);
            gpio.Write(config.errorLedPin, PinValue.Low);

            // Initialise clock
            try
            {
                clock = new Clock(config.clockTickPin, gpio);
                clock.Ticked += Clock_Ticked;

                clock.Open();

                //if (!clock.IsDateTimeValid)
                //{
                //    logger.Error("Scheduling clock time is invalid");
                //    gpio.Write(config.errorLedPin, PinValue.High);
                //    return;
                //}

                startupTime = clock.DateTime;

                logger.Info("Initialised scheduling clock");
                logger.Info("Time is {0}", startupTime.ToString("dd/MM/yyyy HH:mm:ss"));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error while initialising scheduling clock");
                gpio.Write(config.errorLedPin, PinValue.High);
                return;
            }

            // Create data directory
            try
            {
                if (!Directory.Exists(Helpers.DATA_DIRECTORY))
                {
                    Directory.CreateDirectory(Helpers.DATA_DIRECTORY);
                    logger.Info("Created data directory");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error while creating data directory");
                gpio.Write(config.errorLedPin, PinValue.High);
                return;
            }

            // Create data database
            try
            {
                if (!Database.Exists(Database.DatabaseFile.Data))
                {
                    Database.Create(Database.DatabaseFile.Data);
                    logger.Info("Created data database");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error while creating data database");
                gpio.Write(config.errorLedPin, PinValue.High);
                return;
            }

            // Create transmit database
            //try
            //{
            //    if (config.Transmitter.TransmitReports && !Database.Exists(Database.DatabaseFile.Transmit))
            //    {
            //        Database.Create(Database.DatabaseFile.Transmit);
            //        eventLogger.Info("Created transmit database");
            //    }
            //}
            //catch (Exception ex)
            //{
            //    eventLogger.Error(ex, "Error while creating transmit database");
            //    gpio.Write(config.ErrorLedPin, PinValue.High);
            //    return;
            //}

            // Initialise sensors
            sampler = new Sampler(config, gpio);

            if (!sampler.Connect())
            {
                gpio.Write(config.errorLedPin, PinValue.High);
                return;
            }

            // Complete startup procedure
            gpio.Write(config.dataLedPin, PinValue.High);
            gpio.Write(config.errorLedPin, PinValue.High);
            Thread.Sleep(5000);
            gpio.Write(config.dataLedPin, PinValue.Low);
            gpio.Write(config.errorLedPin, PinValue.Low);

            clock.Start();
            logger.Info("Started scheduling clock");

            Console.ReadKey();
        }

        private void Clock_Ticked(object sender, ClockTickedEventArgs e)
        {
            if (!isSampling)
            {
                if (e.Time.Second == 0)
                {
                    if (sampler.Start(e.Time))
                    {
                        isSampling = true;
                        logger.Info("Started sampling");
                    }
                    else gpio.Write(config.errorLedPin, PinValue.High);
                }

                return;
            }

            if (!sampler.Sample(e.Time))
                gpio.Write(config.errorLedPin, PinValue.High);

            if (e.Time.Second == 0)
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

            gpio.Write(config.dataLedPin, PinValue.High);

            Report report = sampler.Report(time);
            Database.WriteReport(report);

            // Ensure the data LED stays on for at least 1.5 seconds
            ledStopwatch.Stop();
            if (ledStopwatch.ElapsedMilliseconds < 1500)
                Thread.Sleep(1500 - (int)ledStopwatch.ElapsedMilliseconds);

            gpio.Write(config.dataLedPin, PinValue.Low);
        }
    }
}
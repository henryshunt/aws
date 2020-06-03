using AWS.Hardware.Sensors;
using AWS.Routines;
using System;
using System.IO;
using System.Threading;

namespace AWS
{
    internal class Program
    {
        private DateTime StartupTime;
        private Configuration Configuration;
        private Clock Clock;

        private bool ShouldSkipSample = true;
        private object TransmitLock = new object();

        // private MAX31865 AirTSensor = new MAX31865();
        // private HTU21D RelHSensor = new HTU21D();
        // private ICA WSpdSensor = new ICA();
        // private IEV2 WDirSensor = new IEV2();
        // private IMSBB SunDSensor = new IMSSB();
        private RR111 RainfallSensor = new RR111();
        // private BMP280 StaPSensor = new BMP280();
        // private MAX31865 ST10Sensor = new MAX31865();
        // private MAX31865 ST30Sensor = new MAX31865();
        // private MAX31865 ST00Sensor = new MAX31865();


        static void Main(string[] args) { new Program().Startup(); }

        public void Startup()
        {
            Helpers.LogEvent("Startup", "Began startup procedure");

            // Load configuration
            try
            {
                Configuration = Configuration.Load(Helpers.CONFIG_FILE);

                if (!Configuration.Validate(Configuration))
                {
                    Helpers.LogEvent("Startup", "Error while validating configuration file");
                    return;
                }
                else Helpers.LogEvent("Startup", "Loaded configuration file");
            }
            catch
            {
                Helpers.LogEvent("Startup", "Error while loading configuration file");
                return;
            }

            // Initialise clock
            try
            {
                Clock = new Clock(Configuration);
                Clock.Ticked += Clock_Ticked;

                Helpers.LogEvent(Clock.DateTime, "Startup", "Initialised scheduling clock");
            }
            catch
            {
                Helpers.LogEvent("Startup", "Error while initialising scheduling clock");
                return;
            }

            StartupTime = Clock.DateTime;

            // Data directory
            try { Directory.CreateDirectory(Helpers.DATA_DIRECTORY); }
            catch
            {
                Helpers.LogEvent(Clock.DateTime, "Startup", "Error while creating data directory");
                return;
            }

            // Data database
            try
            {
                if (!Database.Exists(Database.DatabaseFile.Data))
                {
                    Database.Create(Database.DatabaseFile.Data);
                    Helpers.LogEvent(Clock.DateTime, "Startup", "Created data database");
                }
            }
            catch
            {
                Helpers.LogEvent(Clock.DateTime, "Startup", "Error while creating data database");
                return;
            }

            // Transmit database
            try
            {
                if (Configuration.Transmitter.TransmitReports &&
                    !Database.Exists(Database.DatabaseFile.Transmit))
                {
                    Database.Create(Database.DatabaseFile.Transmit);
                    Helpers.LogEvent(Clock.DateTime, "Startup", "Created transmit database");
                }
            }
            catch
            {
                Helpers.LogEvent(Clock.DateTime, "Startup", "Error while creating transmit database");
                return;
            }


            RainfallSensor.Setup(Configuration.Sensors.Rainfall.Pin);

            Clock.Start();
            Helpers.LogEvent(Clock.DateTime, "Startup", "Started scheduling clock");

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

                //WindSpeedSensor.IsPaused = false;
                RainfallSensor.IsPaused = false;
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
            Console.WriteLine("Sample: " + time.ToString("ss"));

            // Switch interrupt-based sensors to a new bucket right on the minute
            if (time.Second == 0 && !isFirstSample)
            {
                //WindSpeedSensor.SwitchSamplingBucket();
                RainfallSensor.SwitchSamplingBucket();
            }
        }

        private void LogReport(DateTime time)
        {
            Console.WriteLine("Log");

            // Rainfall sensor
            Helpers.SamplingBucket bucket = Helpers.InvertSamplingBucket(RainfallSensor.SamplingBucket);
            Console.WriteLine("Rainfall: " + RainfallSensor.CalculateTotal(bucket) + " mm");
            RainfallSensor.EmptySamplingBucket(bucket);
        }

        private void TransmitReports(DateTime now)
        {
            // Don't start transmitting if we're already transmitting
            if (Monitor.TryEnter(TransmitLock))
            {
                Monitor.Exit(TransmitLock);
            }
        }
    }
}

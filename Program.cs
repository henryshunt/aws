using AWS.Hardware.Sensors;
using AWS.Routines;
using System;
using System.IO;
using System.Threading;
using AWS.Routines.Configuration;

namespace AWS
{
    internal class Program
    {
        private DateTime StartupTime;
        private Configuration Configuration;
        private Clock Clock;

        private bool HasStartedSampling = false;

        private object SampleLock = new object();
        private object TransmitLock = new object();

        // private MAX31865 AirTSensor = new MAX31865();
        // private HTU21D RelHSensor = new HTU21D();
        // private ICA WSpdSensor = new ICA();
        // private IEV2 WDirSensor = new IEV2();
        // private IMSBB SunDSensor = new IMSSB();
        private RR111 RainSensor = new RR111();
        // private BMP280 StaPSensor = new BMP280();
        // private MAX31865 ST10Sensor = new MAX31865();
        // private MAX31865 ST30Sensor = new MAX31865();
        // private MAX31865 ST00Sensor = new MAX31865();


        static void Main(string[] args) { new Program().Startup(); }

        public void Startup()
        {
            Helpers.LogEvent("Startup", "------------------------------------------------");
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


            RainSensor.Setup(Configuration.Sensors.Rainfall.Pin);

            Clock.Start();
            Helpers.LogEvent(Clock.DateTime, "Startup", "Started scheduling clock");

            Console.ReadKey();
        }


        private void Clock_Ticked(object sender, ClockTickedEventArgs e)
        {
            // Start sampling at top of next minute and skip the first sample
            if (!HasStartedSampling && e.TickTime.Second == 0)
            {
                HasStartedSampling = true;

                // WindSensor.IsPaused = false;
                // RainSensor.IsPaused = false;
                return;
            }

            if (!HasStartedSampling) return;
            Console.WriteLine("Alarm: " + e.TickTime.ToString("HH:mm:ss.fffff"));

            //SampleSensors(e.TickTime);
            if (e.TickTime.Second == 0)
            {
                //new Thread(() =>
                //{
                //    LogReport(e.TickTime);
                //    TransmitReports(e.TickTime);
                //}).Start();
            }
        }

        private void SampleSensors(DateTime time)
        {
            if (Monitor.TryEnter(SampleLock)) // Prevent simultaneous samplings
            {
                //Console.WriteLine(DateTime.UtcNow.ToString("HH:mm:ss.fffff"));
                Monitor.Exit(SampleLock);
            }
        }

        private void LogReport(DateTime time)
        {
            Console.WriteLine("Logging procedure");

            // Rain sensor
            Helpers.SamplingBucket bucket = RainSensor.SamplingBucket;
            RainSensor.SwitchSamplingBucket();
            Console.WriteLine("Rainfall: " + RainSensor.CalculateTotal(bucket) + " mm");
            RainSensor.ResetSamplingBucket(bucket);
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

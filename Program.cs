using AWS.Hardware.Sensors;
using AWS.Routines;
using System;
using System.IO;
using System.Threading;
using static AWS.Routines.Helpers;

namespace AWS
{
    internal class Program
    {
        private DateTime StartupTime;
        private Configuration Configuration = new Configuration();
        private SchedulingClock SchedulingClock;

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
            StartupTime = DateTime.UtcNow;
            LogEvent(LoggingSource.Startup, "Startup procedure started");

            if (!Configuration.Load(CONFIG_FILE))
            {
                LogEvent(LoggingSource.Startup, "Error while loading the configuration file");
                return;
            }
            else LogEvent(LoggingSource.Startup, "Configuration file successfully loaded");

            try { Directory.CreateDirectory(DATA_DIRECTORY); }
            catch
            {
                LogEvent(LoggingSource.Startup, "Error while creating the data directory");
                return;
            }

            try
            {
                SchedulingClock = new SchedulingClock(Configuration);
                SchedulingClock.Ticked += SchedulingClock_Ticked;
            }
            catch
            {
                LogEvent(LoggingSource.Startup, "Error while initialising the scheduling clock");
                return;
            }

            RainSensor.Setup(4);

            SchedulingClock.Start();
        }


        private void SchedulingClock_Ticked(object sender, SchedulingClockTickedEventArgs e)
        {
            // Start sampling at top of next minute and skip the first sample
            if (!HasStartedSampling && e.TickTime.Second == 0)
            {
                HasStartedSampling = true;

                // WindSensor.IsPaused = false;
                // RainSensor.IsPaused = false;
                return;
            }

            SampleSensors(e.TickTime);
            if (e.TickTime.Second == 0)
            {
                new Thread(() =>
                {
                    LogReport(e.TickTime);
                    TransmitReports(e.TickTime);
                }).Start();
            }
        }

        private void SampleSensors(DateTime time)
        {
            if (Monitor.TryEnter(SampleLock)) // Prevent simultaneous samplings
            {
                Console.WriteLine(DateTime.UtcNow.ToString("HH:mm:ss.fffff"));
                Monitor.Exit(SampleLock);
            }
        }

        private void LogReport(DateTime time)
        {
            Console.WriteLine("Logging procedure");

            // Rain sensor
            SamplingBucket bucket = RainSensor.SamplingBucket;
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

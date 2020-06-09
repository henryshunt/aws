using AWS.Hardware.Sensors;
using AWS.Routines;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using static AWS.Hardware.Sensors.Satellite;
using static AWS.Routines.Helpers;

namespace AWS
{
    internal class Program
    {
        private DateTime StartupTime;
        private Configuration Configuration;
        private Clock Clock;

        private bool ShouldSkipSample = true;
        private object TransmitLock = new object();

        private Dictionary<int, Satellite> Satellites = new Dictionary<int, Satellite>();
        // private MAX31865 AirTSensor = new MAX31865();
        // private HTU21D RelHSensor = new HTU21D();
        private Inspeed8PulseAnemometer WindSpeedSensor = new Inspeed8PulseAnemometer();
        private InspeedWindVane WindDirectionSensor = new InspeedWindVane();
        // private IMSBB SunDSensor = new IMSSB();
        private RainwiseRainew111 RainfallSensor = new RainwiseRainew111();
        // private BMP280 StaPSensor = new BMP280();
        // private MAX31865 ST10Sensor = new MAX31865();
        // private MAX31865 ST30Sensor = new MAX31865();
        // private MAX31865 ST00Sensor = new MAX31865();


        static void Main(string[] args) { new Program().Startup(); }

        public void Startup()
        {
            LogEvent("Startup", "Began startup procedure");

            // Load configuration
            try
            {
                Configuration = Configuration.Load(CONFIG_FILE);

                if (!Configuration.Validate(Configuration))
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

            // Initialise clock
            try
            {
                Clock = new Clock(Configuration);
                Clock.Ticked += Clock_Ticked;

                LogEvent(Clock.DateTime, "Startup", "Initialised scheduling clock");
            }
            catch
            {
                LogEvent("Startup", "Error while initialising scheduling clock");
                return;
            }

            StartupTime = Clock.DateTime;

            // Data directory
            try { Directory.CreateDirectory(DATA_DIRECTORY); }
            catch
            {
                LogEvent(Clock.DateTime, "Startup", "Error while creating data directory");
                return;
            }

            // Data database
            try
            {
                if (!Database.Exists(Database.DatabaseFile.Data))
                {
                    Database.Create(Database.DatabaseFile.Data);
                    LogEvent(Clock.DateTime, "Startup", "Created data database");
                }
            }
            catch
            {
                LogEvent(Clock.DateTime, "Startup", "Error while creating data database");
                return;
            }

            // Transmit database
            try
            {
                if (Configuration.Transmitter.TransmitReports &&
                    !Database.Exists(Database.DatabaseFile.Transmit))
                {
                    Database.Create(Database.DatabaseFile.Transmit);
                    LogEvent(Clock.DateTime, "Startup", "Created transmit database");
                }
            }
            catch
            {
                LogEvent(Clock.DateTime, "Startup", "Error while creating transmit database");
                return;
            }


            InitialiseSensors();

            Clock.Start();
            LogEvent(Clock.DateTime, "Startup", "Started scheduling clock");
            Console.ReadKey();
        }
        private void InitialiseSensors()
        {
            Dictionary<int, SatelliteConfiguration> configs = new Dictionary<int, SatelliteConfiguration>();
            foreach (int satelliteId in Configuration.Sensors.SatelliteIDs)
            {
                Satellites.Add(satelliteId, new Satellite());
                configs.Add(satelliteId, new SatelliteConfiguration());
            }

            if (Configuration.Sensors.WindSpeed.Enabled)
            {
                if (Configuration.Sensors.WindSpeed.SatelliteID != null)
                {
                    configs[(int)Configuration.Sensors.WindSpeed.SatelliteID].WindSpeedEnabled = true;
                    configs[(int)Configuration.Sensors.WindSpeed.SatelliteID].WindSpeedPin =
                        (int)Configuration.Sensors.WindSpeed.Pin;
                    WindSpeedSensor.Initialise(Satellites[(int)Configuration.Sensors.WindSpeed.SatelliteID]);
                }
            }

            if (Configuration.Sensors.WindDirection.Enabled)
            {
                if (Configuration.Sensors.WindDirection.SatelliteID != null)
                {
                    configs[(int)Configuration.Sensors.WindDirection.SatelliteID].WindDirectionEnabled = true;
                    configs[(int)Configuration.Sensors.WindDirection.SatelliteID].WindDirectionPin =
                        (int)Configuration.Sensors.WindDirection.Pin;
                    WindDirectionSensor.Initialise(Satellites[(int)Configuration.Sensors.WindDirection.SatelliteID]);
                }
            }

            RainfallSensor.Initialise((int)Configuration.Sensors.Rainfall.Pin);

            foreach (KeyValuePair<int, Satellite> satellite in Satellites)
                satellite.Value.Initialise(satellite.Key, configs[satellite.Key]);
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

                foreach (KeyValuePair<int, Satellite> satellite in Satellites)
                    satellite.Value.StartSensors();

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
            //Console.WriteLine("Sample: " + time.ToString("ss"));

            foreach (KeyValuePair<int, Satellite> satellite in Satellites)
                satellite.Value.ReadSensors();

            // Switch interrupt-based sensors to a new bucket right on the minute
            if (time.Second == 0 && !isFirstSample)
            {
                //WindSpeedSensor.SwitchSamplingBucket();
                RainfallSensor.SwitchSamplingBucket();
            }
        }
        private void LogReport(DateTime time)
        {
            Report report = new Report(time);

            // Rainfall sensor
            SamplingBucket samplingBucket = InvertSamplingBucket(RainfallSensor.SamplingBucket);
            report.Rainfall = RainfallSensor.CalculateTotal(samplingBucket);
            RainfallSensor.EmptySamplingBucket(samplingBucket);

            //Console.WriteLine("Log");
            Console.WriteLine("Rainfall: " + report.Rainfall + " mm");

            Database.WriteReport(report);
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

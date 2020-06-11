using AWS.Hardware.Sensors;
using AWS.Routines;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
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

        private Dictionary<int, Satellite> Satellites = new Dictionary<int, Satellite>();
        private BME680 BME680Sensor = new BME680();
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
            // Create a blank configuration for each satellite mentioned in the main configuration
            Dictionary<int, SatelliteConfiguration> satConfigs = new Dictionary<int, SatelliteConfiguration>();
            foreach (int satelliteId in Configuration.Sensors.SatelliteIDs)
                satConfigs.Add(satelliteId, new SatelliteConfiguration());

            BME680Sensor.Initialise(false);
            //if (Configuration.Sensors.AirTemperature.Enabled)
            //{
            //    int? satelliteId = Configuration.Sensors.AirTemperature.SatelliteID;
            //    if (satelliteId != null)
            //    {
            //        satConfigs[(int)satelliteId].AirTemperatureEnabled = true;
            //        BME680Sensor.Initialise(true);
            //    }
            //    else BME680Sensor.Initialise(false);
            //}

            //if (Configuration.Sensors.WindSpeed.Enabled)
            //{
            //    int? satelliteId = Configuration.Sensors.WindSpeed.SatelliteID;
            //    if (satelliteId != null)
            //    {
            //        satConfigs[(int)satelliteId].WindSpeedEnabled = true;
            //        satConfigs[(int)satelliteId].WindSpeedPin = (int)Configuration.Sensors.WindSpeed.Pin;
            //        WindSpeedSensor.Initialise();
            //    }
            //    else WindSpeedSensor.Initialise((int)Configuration.Sensors.WindSpeed.Pin);
            //}

            //if (Configuration.Sensors.WindDirection.Enabled)
            //{
            //    int? satelliteId = Configuration.Sensors.WindDirection.SatelliteID;
            //    if (satelliteId != null)
            //    {
            //        satConfigs[(int)satelliteId].WindDirectionEnabled = true;
            //        satConfigs[(int)satelliteId].WindDirectionPin = (int)Configuration.Sensors.WindDirection.Pin;
            //        WindDirectionSensor.Initialise(true);
            //    }
            //    else WindDirectionSensor.Initialise(false, (int)Configuration.Sensors.WindDirection.Pin);
            //}

            if (Configuration.Sensors.Rainfall.Enabled)
            {
                int? satelliteId = Configuration.Sensors.Rainfall.SatelliteID;
                if (satelliteId != null)
                {
                    satConfigs[(int)satelliteId].RainfallEnabled = true;
                    satConfigs[(int)satelliteId].RainfallPin = (int)Configuration.Sensors.Rainfall.Pin;
                    RainfallSensor.Initialise();
                }
                else RainfallSensor.Initialise((int)Configuration.Sensors.Rainfall.Pin);
            }


            // Initialise each satellite with its finalised configurations
            //foreach (int satelliteId in Configuration.Sensors.SatelliteIDs)
            //{
            //    Satellite satellite = new Satellite();
            //    satellite.Initialise(satelliteId, satConfigs[satelliteId]);
            //    Satellites.Add(satelliteId, satellite);
            //}
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
            //foreach (KeyValuePair<int, Satellite> satellite in Satellites)
            //{
            //    satellite.Value.ReadSensors();
            //}

            if (time.Second == 0)
            {
                //WindSpeedSensor.WindSpeedStore.SwapValueBuckets();
                RainfallSensor.RainfallStore.SwapValueBucket();

                BME680Sensor.TemperatureStore.SwapValueBucket();
                BME680Sensor.RelativeHumidityStore.SwapValueBucket();
                BME680Sensor.PressureStore.SwapValueBucket();
            }

            Thread BME680Thread = BME680Sensor.SampleDevice();

            // Don't continue until all sensors have completed sampling
            BME680Thread.Join();
        }
        private void LogReport(DateTime time)
        {
            Report report = new Report(time);

            //// Rainfall sensor
            //SamplingBucket samplingBucket = InvertSamplingBucket(RainfallSensor.SamplingBucket);
            //report.Rainfall = RainfallSensor.CalculateTotal(samplingBucket);
            //RainfallSensor.EmptySamplingBucket(samplingBucket);
            //Console.WriteLine("Rainfall: " + report.Rainfall + " mm");

            report.AirTemperature = BME680Sensor.TemperatureStore.InactiveValueBucket.Average();
            BME680Sensor.TemperatureStore.InactiveValueBucket.Clear();
            report.RelativeHumidity = BME680Sensor.RelativeHumidityStore.InactiveValueBucket.Average();
            BME680Sensor.RelativeHumidityStore.InactiveValueBucket.Clear();
            report.StationPressure = BME680Sensor.PressureStore.InactiveValueBucket.Average();
            BME680Sensor.PressureStore.InactiveValueBucket.Clear();

            Console.WriteLine(string.Format("T: {0}   H: {1}   P: {2}", Math.Round((double)report.AirTemperature, 2),
                Math.Round((double)report.RelativeHumidity, 2), Math.Round((double)report.StationPressure, 2)));

            Database.WriteReport(report);
        }
        private void TransmitReports(DateTime now)
        {

        }
    }
}

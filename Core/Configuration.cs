using Aws.Routines;
using Newtonsoft.Json.Linq;
using System;
using System.IO;

namespace Aws.Core
{
    internal class Configuration
    {
        public string FilePath { get; private set; }

        public int dataLedPin { get; private set; }
        public int errorLedPin { get; private set; }
        public int clockTickPin { get; private set; }
        public dynamic sensors { get; private set; }
        public TimeZoneInfo timeZone { get; private set; }
        public dynamic Gps { get; private set; }

        public Configuration(string filePath)
        {
            FilePath = filePath;
        }


        public bool Load()
        {
            dynamic jsonObject = JObject.Parse(File.ReadAllText(FilePath));

            //if (Validate(jsonObject))
            //{
            dataLedPin = jsonObject.dataLedPin;
            errorLedPin = jsonObject.errorLedPin;
            clockTickPin = jsonObject.clockTickPin;
            sensors = jsonObject.sensors;

            if (sensors.satellite.i8pa.enabled == true || sensors.satellite.iev2.enabled == true)
                sensors.satellite.enabled = true;

            timeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/London");

            Gps = JObject.Parse(File.ReadAllText(Helpers.GPS_FILE));
            return true;
            //}
            //else return false;
        }

        private bool Validate(dynamic jsonObject)
        {
            try
            {
                if ((jsonObject.dataLedPin as JValue).Value.GetType() != typeof(long))
                    return false;
                if ((jsonObject.errorLedPin as JValue).Value.GetType() != typeof(long))
                    return false;
                if ((jsonObject.clockTickPin as JValue).Value.GetType() != typeof(long))
                    return false;

                if ((jsonObject.sensors.airTemperature.enabled as JValue).Value.GetType() != typeof(bool))
                    return false;
                if ((jsonObject.sensors.relativeHumidity.enabled as JValue).Value.GetType() != typeof(bool))
                    return false;
                if ((jsonObject.sensors.barometricPressure.enabled as JValue).Value.GetType() != typeof(bool))
                    return false;

                if ((jsonObject.sensors.satellite.windSpeed.enabled as JValue).Value.GetType() != typeof(bool))
                    return false;
                if (jsonObject.sensors.satellite.windSpeed.enabled == true &&
                    (jsonObject.sensors.satellite.windSpeed.pin as JValue).Value.GetType() != typeof(long))
                {
                    return false;
                }

                if ((jsonObject.sensors.satellite.windDir.enabled as JValue).Value.GetType() != typeof(bool))
                    return false;
                if (jsonObject.sensors.satellite.windDir.enabled == true &&
                    (jsonObject.sensors.satellite.windDir.pin as JValue).Value.GetType() != typeof(long))
                {
                    return false;
                }

                if ((jsonObject.sensors.rainfall.enabled as JValue).Value.GetType() != typeof(bool))
                    return false;
                if (jsonObject.sensors.rainfall.enabled == true &&
                    (jsonObject.sensors.rainfall.pin as JValue).Value.GetType() != typeof(long))
                {
                    return false;
                }

                if ((jsonObject.sensors.sunshineDuration.enabled as JValue).Value.GetType() != typeof(bool))
                    return false;
                if (jsonObject.sensors.sunshineDuration.enabled == true &&
                    (jsonObject.sensors.sunshineDuration.pin as JValue).Value.GetType() != typeof(long))
                {
                    return false;
                }

                if ((jsonObject.sensors.soilTemperature10.enabled as JValue).Value.GetType() != typeof(bool))
                    return false;
                if ((jsonObject.sensors.soilTemperature30.enabled as JValue).Value.GetType() != typeof(bool))
                    return false;
                if ((jsonObject.sensors.soilTemperature100.enabled as JValue).Value.GetType() != typeof(bool))
                    return false;

                return true;
            }
            catch (NullReferenceException)
            {
                return false;
            }
        }
    }
}
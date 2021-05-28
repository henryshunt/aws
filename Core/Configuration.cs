using Aws.Misc;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NJsonSchema;
using NJsonSchema.Validation;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static Aws.Misc.Utilities;

namespace Aws.Core
{
    /// <summary>
    /// Represents the program's configuration data.
    /// </summary>
    internal class Configuration
    {
        public int dataLedPin { get; private set; }
        public int errorLedPin { get; private set; }
        public int clockTickPin { get; private set; }
        public dynamic position { get; private set; }
        public dynamic sensors { get; private set; }
        public dynamic uploader { get; private set; }

        /// <summary>
        /// Initialises a new instance of the <see cref="Configuration"/> class.
        /// </summary>
        public Configuration() { }

        /// <summary>
        /// Loads configuration data from a JSON file located at <see cref="CONFIG_FILE"/>.
        /// </summary>
        /// <exception cref="ConfigurationSchemaException">
        /// Thrown if the configuration data does not conform to the required schema.
        /// </exception>
        public async Task LoadAsync()
        {
            string json = File.ReadAllText(CONFIG_FILE);

            JsonSchema schema =
                await JsonSchema.FromFileAsync("Resources/ConfigurationSchema.json");
            ICollection<ValidationError> errors = schema.Validate(json);

            if (errors.Count > 0)
            {
                throw new ConfigurationSchemaException(
                    "Configuration data does not conform to the required schema: " +
                    errors.ElementAt(0).ToString());
            }

            dynamic jsonObject = JsonConvert.DeserializeObject<ExpandoObject>(json,
                new ExpandoObjectConverter());

            var sensorsDict = (IDictionary<string, object>)jsonObject.sensors;

            if (sensorsDict.ContainsKey("satellite"))
            {
                var satelliteDict = (IDictionary<string, object>)sensorsDict["satellite"];

                if (!satelliteDict.ContainsKey("windSpeed") &&
                    !satelliteDict.ContainsKey("windDir") &&
                    !satelliteDict.ContainsKey("sunDur"))
                {
                    throw new ConfigurationSchemaException(
                        "sensors.satellite must contain at least one of windSpeed, windDir or sunDur");
                }
            }

            dataLedPin = (int)jsonObject.dataLedPin;
            errorLedPin = (int)jsonObject.errorLedPin;
            clockTickPin = (int)jsonObject.clockTickPin;
            position = jsonObject.position;
            position.timeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/London");
            sensors = jsonObject.sensors;
            uploader = jsonObject.uploader;
        }

        /// <summary>
        /// Determines whether a sensor is enabled in the configuration data.
        /// </summary>
        /// <param name="sensor">
        /// The sensor to check.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the sensor is enabled, otherwise <see langword="false"/>.
        /// </returns>
        public bool IsSensorEnabled(AwsSensor sensor)
        {
            var sensorsDict = (IDictionary<string, object>)sensors;

            switch (sensor)
            {
                case AwsSensor.AirTemperature:
                    return sensorsDict.ContainsKey("airTemp");
                case AwsSensor.RelativeHumidity:
                    return sensorsDict.ContainsKey("relHum");
                case AwsSensor.Bme680:
                    return sensorsDict.ContainsKey("bme680");
                case AwsSensor.Satellite:
                    return sensorsDict.ContainsKey("satellite");
                case AwsSensor.WindSpeed:
                    {
                        if (sensorsDict.ContainsKey("satellite"))
                        {
                            var satelliteDict = (IDictionary<string, object>)sensorsDict["satellite"];
                            return satelliteDict.ContainsKey("windSpeed");
                        }
                        else return false;
                    }
                case AwsSensor.WindDirection:
                    {
                        if (sensorsDict.ContainsKey("satellite"))
                        {
                            var satelliteDict = (IDictionary<string, object>)sensorsDict["satellite"];
                            return satelliteDict.ContainsKey("windDir");
                        }
                        else return false;
                    }
                case AwsSensor.SunshineDuration:
                    {
                        if (sensorsDict.ContainsKey("satellite"))
                        {
                            var satelliteDict = (IDictionary<string, object>)sensorsDict["satellite"];
                            return satelliteDict.ContainsKey("sunDur");
                        }
                        else return false;
                    }
                case AwsSensor.Rainfall:
                    return sensorsDict.ContainsKey("rainfall");
                default: return false;
            }
        }
    }
}
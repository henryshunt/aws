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
    /// Represents the configuration data for the AWS system.
    /// </summary>
    internal class Configuration
    {
        public int dataLedPin { get; private set; }
        public int errorLedPin { get; private set; }
        public int clockTickPin { get; private set; }
        public dynamic position { get; private set; }
        public dynamic sensors { get; private set; }
        public dynamic transmitter { get; private set; }

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

                if (!satelliteDict.ContainsKey("i8pa") &&
                    !satelliteDict.ContainsKey("i8pa") &&
                    !satelliteDict.ContainsKey("i8pa"))
                {
                    throw new ConfigurationSchemaException(
                        "sensors.satellite must contain at least one of i8pa, iev2 or isds");
                }
            }

            dataLedPin = (int)jsonObject.dataLedPin;
            errorLedPin = (int)jsonObject.errorLedPin;
            clockTickPin = (int)jsonObject.clockTickPin;
            position = jsonObject.position;
            position.timeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/London");
            sensors = jsonObject.sensors;
            transmitter = jsonObject.transmitter;
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
                case AwsSensor.Mcp9808:
                    return sensorsDict.ContainsKey("mcp9808");
                case AwsSensor.Bme680:
                    return sensorsDict.ContainsKey("bme680");
                case AwsSensor.Satellite:
                    return sensorsDict.ContainsKey("satellite");
                case AwsSensor.I8pa:
                    {
                        if (sensorsDict.ContainsKey("satellite"))
                        {
                            var satelliteDict = (IDictionary<string, object>)sensorsDict["satellite"];
                            return satelliteDict.ContainsKey("i8pa");
                        }
                        else return false;
                    }
                case AwsSensor.Iev2:
                    {
                        if (sensorsDict.ContainsKey("satellite"))
                        {
                            var satelliteDict = (IDictionary<string, object>)sensorsDict["satellite"];
                            return satelliteDict.ContainsKey("iev2");
                        }
                        else return false;
                    }
                case AwsSensor.Isds:
                    {
                        if (sensorsDict.ContainsKey("satellite"))
                        {
                            var satelliteDict = (IDictionary<string, object>)sensorsDict["satellite"];
                            return satelliteDict.ContainsKey("isds");
                        }
                        else return false;
                    }
                case AwsSensor.Rr111:
                    return sensorsDict.ContainsKey("rr111");
                default: return false;
            }
        }
    }
}
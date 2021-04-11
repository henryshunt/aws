using Aws.Routines;
using Newtonsoft.Json.Linq;
using NJsonSchema;
using NJsonSchema.Validation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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
        public TimeZoneInfo timeZone { get; private set; }
        public dynamic sensors { get; private set; }

        /// <summary>
        /// Initialises a new instance of the <see cref="Configuration"/> class.
        /// </summary>
        public Configuration() { }

        /// <summary>
        /// Loads configuration data from a JSON file located at <see cref="Helpers.CONFIG_FILE"/>.
        /// </summary>
        /// <exception cref="ConfigurationSchemaException">
        /// Thrown if the configuration data does not conform to the required schema.
        /// </exception>
        public async Task LoadAsync()
        {
            string json = File.ReadAllText(Helpers.CONFIG_FILE);

            JsonSchema schema =
                await JsonSchema.FromFileAsync("Resources/ConfigurationSchema.json");
            ICollection<ValidationError> errors = schema.Validate(json);

            if (errors.Count > 0)
            {
                throw new ConfigurationSchemaException(
                    "Configuration data does not conform to the required schema: " +
                    errors.ElementAt(0).ToString());
            }

            dynamic jsonObject = JObject.Parse(json);

            dataLedPin = jsonObject.dataLedPin;
            errorLedPin = jsonObject.errorLedPin;
            clockTickPin = jsonObject.clockTickPin;
            position = jsonObject.position;
            timeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/London");
            sensors = jsonObject.sensors;

            if ((bool)sensors.satellite.i8pa.enabled || (bool)sensors.satellite.iev2.enabled)
                sensors.satellite.enabled = true;
        }
    }
}
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
    public class Configuration
    {
        public int dataLedPin { get; private set; }
        public int errorLedPin { get; private set; }
        public int clockTickPin { get; private set; }
        public dynamic gps { get; private set; } = null;
        public TimeZoneInfo timeZone { get; private set; }
        public dynamic sensors { get; private set; } = null;

        /// <summary>
        /// Initialises a new instance of the <see cref="Configuration"/> class.
        /// </summary>
        public Configuration() { }

        /// <summary>
        /// Loads configuration data from a JSON file.
        /// </summary>
        /// <param name="filePath">
        /// The path to the file containing the JSON configuration data to load.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the configuration data was successfully loaded, otherwise
        /// <see langword="false"/>.
        /// </returns>
        public async Task<bool> LoadAsync(string filePath)
        {
            string json = File.ReadAllText(filePath);

            if (!await ValidateAsync(json))
                return false;

            dynamic jsonObject = JObject.Parse(json);

            dataLedPin = jsonObject.dataLedPin;
            errorLedPin = jsonObject.errorLedPin;
            clockTickPin = jsonObject.clockTickPin;
            sensors = jsonObject.sensors;

            if ((bool)sensors.satellite.i8pa.enabled || (bool)sensors.satellite.iev2.enabled)
                sensors.satellite.enabled = true;

            return true;
        }

        /// <summary>
        /// Determines whether a JSON string matches the configuration validation schema.
        /// </summary>
        /// <param name="json">
        /// The JSON string to check.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the JSON string matches the configuration validation schema,
        /// otherwise <see langword="false"/>.
        /// </returns>
        private async Task<bool> ValidateAsync(string json)
        {
            JsonSchema schema =
                await JsonSchema.FromFileAsync("Resources/ConfigurationSchema.json");

            ICollection<ValidationError> errors = schema.Validate(json);

            if (errors.Count > 0)
            {
                Helpers.LogEvent(null, nameof(Configuration),
                    "Invalid: " + errors.ElementAt(0).ToString());
            }

            return errors.Count == 0;
        }

        /// <summary>
        /// Loads GPS data from a JSON file.
        /// </summary>
        /// <param name="filePath">
        /// The path to the file containing the JSON GPS data to load.
        /// </param>
        public void LoadGps(string filePath)
        {
            gps = JObject.Parse(File.ReadAllText(filePath));
            timeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/London");
        }
    }
}
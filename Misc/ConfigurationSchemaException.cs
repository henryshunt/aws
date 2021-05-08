using System;

namespace Aws.Core
{
    /// <summary>
    /// The exception that is thrown when configuration data loaded by the <see cref="Configuration"/> class does not
    /// match the required schema.
    /// </summary>
    internal class ConfigurationSchemaException : Exception
    {
        /// <summary>
        /// Initialises a new instance of the <see cref="ConfigurationSchemaException"/> class.
        /// </summary>
        /// <param name="message">
        /// The error message that explains the reason for the exception.
        /// </param>
        public ConfigurationSchemaException(string message)
            : base(message) { }
    }
}

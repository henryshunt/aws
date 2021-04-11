using System;

namespace Aws.Core
{
    public class ConfigurationSchemaException : Exception
    {
        public ConfigurationSchemaException(string message)
            : base(message) { }
    }
}

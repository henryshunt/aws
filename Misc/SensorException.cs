using System;

namespace Aws.Misc
{
    public class SensorException : Exception
    {
        public SensorException(string message)
            : base(message)
        {

        }

        public SensorException(string message, Exception inner)
            : base(message, inner)
        {

        }
    }
}

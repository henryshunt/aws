using System;

namespace Aws.Hardware
{
    /// <summary>
    /// An exception that is thrown by a <see cref="Satellite"/> device.
    /// </summary>
    internal class SatelliteException : Exception
    {
        /// <summary>
        /// Initialises a new instance of the <see cref="SatelliteException"/> class.
        /// </summary>
        /// <param name="message">
        /// The error message that explains the reason for the exception.
        /// </param>
        public SatelliteException(string message)
            : base(message) { }
    }
}

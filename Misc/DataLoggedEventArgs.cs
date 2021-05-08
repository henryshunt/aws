using System;

namespace Aws.Core
{
    /// <summary>
    /// Represents event data about data that has been logged by the <see cref="DataLogger"/> class.
    /// </summary>
    internal class DataLoggedEventArgs : EventArgs
    {
        /// <summary>
        /// The time of the data that has been logged.
        /// </summary>
        public DateTime Time { get; private set; }

        /// <summary>
        /// Initialises a new instance of the <see cref="DataLoggedEventArgs"/> class.
        /// </summary>
        /// <param name="time">
        /// The time of the data that has been logged.
        /// </param>
        public DataLoggedEventArgs(DateTime time)
        {
            Time = time;
        }
    }
}

using System;

namespace Aws.Core
{
    /// <summary>
    /// Represents event data about a tick from the <see cref="Clock"/> class.
    /// </summary>
    internal class ClockTickedEventArgs : EventArgs
    {
        /// <summary>
        /// The time of the clock tick.
        /// </summary>
        public DateTime Time { get; private set; }

        /// <summary>
        /// Initialises a new instance of the <see cref="ClockTickedEventArgs"/> class.
        /// </summary>
        /// <param name="time">
        /// The time of the clock tick.
        /// </param>
        public ClockTickedEventArgs(DateTime time)
        {
            Time = time;
        }
    }
}

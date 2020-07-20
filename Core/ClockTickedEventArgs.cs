using System;

namespace AWS.Core
{
    internal class ClockTickedEventArgs : EventArgs
    {
        public DateTime TickTime { get; private set; }

        public ClockTickedEventArgs(DateTime tickTime)
        {
            TickTime = tickTime;
        }
    }
}

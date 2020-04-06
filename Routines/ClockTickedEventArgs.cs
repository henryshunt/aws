using System;

namespace AWS.Routines
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

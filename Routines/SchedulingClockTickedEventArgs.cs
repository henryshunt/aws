using System;
using System.Collections.Generic;
using System.Text;

namespace AWS.Routines
{
    internal class SchedulingClockTickedEventArgs : EventArgs
    {
        public DateTime TickTime { get; private set; }

        public SchedulingClockTickedEventArgs(DateTime tickTime)
        {
            TickTime = tickTime;
        }
    }
}

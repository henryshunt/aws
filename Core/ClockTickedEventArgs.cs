using System;

namespace AWS.Core
{
    internal class ClockTickedEventArgs : EventArgs
    {
        public DateTime Time { get; private set; }

        public ClockTickedEventArgs(DateTime time)
        {
            Time = time;
        }
    }
}

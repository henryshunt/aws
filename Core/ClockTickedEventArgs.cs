using System;

namespace AWS.Core
{
    internal class ClockTickedEventArgs : EventArgs
    {
        public DateTime DateTime { get; private set; }

        public ClockTickedEventArgs(DateTime dateTime)
        {
            DateTime = dateTime;
        }
    }
}

using System;

namespace Aws.Core
{
    public class DataLoggerEventArgs : EventArgs
    {
        public DateTime Time { get; private set; }

        public DataLoggerEventArgs(DateTime time)
        {
            Time = time;
        }
    }
}

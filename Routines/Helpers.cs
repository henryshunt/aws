using System;
using System.Collections.Generic;
using System.Text;

namespace AWS.Routines
{
    internal static class Helpers
    {
        public enum ExitAction
        {
            None,
            Terminate, // Terminates the software
            Shutdown,  // Terminates the software and shuts down the station computer
            Restart    // Terminates the software and restarts the station computer
        }

        public enum SamplingBucket
        {
            Bucket1,
            Bucket2
        }

        public static void LogEvent(string source, string description)
        {
            Console.WriteLine(DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss.f") + " -> " + source + ": " + description);
        }
    }
}

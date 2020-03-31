using System;
using System.Collections.Generic;
using System.Text;

namespace AWS.Routines
{
    internal static class Helpers
    {
        public static string LOGGING_FILE = "/var/logs/aws.log";
        public static string CONFIG_FILE = "/etc/aws.ini";
        public static string DATA_DIRECTORY = "/var/aws";

        public enum ExitAction
        {
            None,
            Terminate, // Terminates the software
            Shutdown,  // Terminates the software and shuts down the station computer
            Restart    // Terminates the software and restarts the station computer
        }

        public enum SamplingBucket { Bucket1, Bucket2 }

        public static void LogEvent(LoggingSource source, string description)
        {
            Console.WriteLine(DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss.f") +
                " -> " + source.ToString() + ": " + description);
        }

        public enum LoggingSource { Startup, Logger, Transmitter }
    }
}

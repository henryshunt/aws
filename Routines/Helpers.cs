using System;
using System.Collections.Generic;
using System.Text;

namespace AWS.Routines
{
    internal static class Helpers
    {
        public static string LOGGING_FILE = "/var/logs/aws.log";
        public static string CONFIG_FILE = "/etc/aws.json";
        public static string DATA_DIRECTORY = "/var/aws/";

        public static void LogEvent(string source, string description)
        {
            Console.WriteLine(string.Format("                       {0}: {1}", source, description));
        }
        public static void LogEvent(string source, string description, Exception exception)
        {
            Console.WriteLine(string.Format("                       {0}: {1}", source, description));
            Console.WriteLine("                           " + exception.Message);

        }
        public static void LogEvent(DateTime time, string source, string description)
        {
            Console.WriteLine(string.Format(
                "{0} -> {1}: {2}", time.ToString("dd/MM/yyyy HH:mm:ss"), source, description));
        }
        public static void LogEvent(DateTime time, string source, string description, Exception exception)
        {
            Console.WriteLine(string.Format(
                "{0} -> {1}: {2}", time.ToString("dd/MM/yyyy HH:mm:ss"), source, description));
            Console.WriteLine("                           " + exception.Message);
        }

        public enum ExitAction
        {
            None,
            Terminate, // Terminates the software
            Shutdown,  // Terminates the software and shuts down the station computer
            Restart    // Terminates the software and restarts the station computer
        }
    }
}

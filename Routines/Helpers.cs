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

        public enum SamplingBucket { Bucket1, Bucket2 }
        public enum ValueBucket { Bucket1, Bucket2 }

        public class Report
        {
            public DateTime Time { get; set; }
            public double? AirTemperature { get; set; } = null;
            public double? RelativeHumidity { get; set; } = null;
            public float? DewPoint { get; set; } = null;
            public float? WindSpeed { get; set; } = null;
            public float? WindDirection { get; set; } = null;
            public float? WindGustSpeed { get; set; } = null;
            public float? WindGustDirection { get; set; } = null;
            public double? Rainfall { get; set; } = null;
            public double? StationPressure { get; set; } = null;
            public float? MSLPressure { get; set; } = null;
            public float? SoilTemperature10 { get; set; } = null;
            public float? SoilTemperature30 { get; set; } = null;
            public float? SoilTemperature100 { get; set; } = null;

            public Report(DateTime time)
            {
                Time = time;
            }
        }

        public static SamplingBucket InvertSamplingBucket(SamplingBucket samplingBucket)
        {
            if (samplingBucket == SamplingBucket.Bucket1)
                return SamplingBucket.Bucket2;
            else return SamplingBucket.Bucket1;
        }

        public static ValueBucket InvertValueBucket(ValueBucket valueBucket)
        {
            if (valueBucket == ValueBucket.Bucket1)
                return ValueBucket.Bucket2;
            else return ValueBucket.Bucket1;
        }
    }
}

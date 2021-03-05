using System;
using System.Collections.Generic;
using System.Linq;

namespace Aws.Routines
{
    internal static class Helpers
    {
        public static string LOGGING_FILE = "/var/logs/aws.log";
        public static string CONFIG_FILE = "/etc/aws.json";
        public static string DATA_DIRECTORY = "/var/aws/";

        public enum ExitAction
        {
            None,
            Terminate, // Terminates the software
            Shutdown,  // Terminates the software and shuts down the station computer
            Restart    // Terminates the software and restarts the station computer
        }

        public static void LogEvent(DateTime? time, string source, string message)
        {
            string line = "";

            if (time != null)
                line += ((DateTime)time).ToString("[yyyy-MM-dd HH:mm:ss]");
            else line += "[      NO TIME      ]";

            line += string.Format(" {0}: {1}", source, message);
            Console.WriteLine(line);
        }

        public static double AverageWindDirection(List<Vector> vectors)
        {
            List<double> V_east = new List<double>();
            List<double> V_north = new List<double>();

            foreach (Vector vector in vectors)
            {
                V_east.Add(vector.Magnitude * Math.Sin(vector.Direction * Math.PI / 180));
                V_north.Add(vector.Magnitude * Math.Cos(vector.Direction * Math.PI / 180));
            }

            double ve = V_east.Sum() / vectors.Count;
            double vn = V_north.Sum() / vectors.Count;

            double mean_WD = Math.Atan2(ve, vn) * 180 / Math.PI;
            mean_WD = (360 + mean_WD) % 360;

            return mean_WD;
        }
    }
}

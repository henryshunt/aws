using System;
using System.Collections.Generic;
using System.Linq;

namespace AWS.Routines
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

        public static double AverageWindDirection(List<(double, double)> vectors)
        {
            List<double> V_east = new List<double>();
            List<double> V_north = new List<double>();

            foreach ((double, double) vector in vectors)
            {
                V_east.Add(vector.Item1 * Math.Sin(vector.Item2 * Math.PI / 180));
                V_north.Add(vector.Item1 * Math.Cos(vector.Item2 * Math.PI / 180));
            }

            double ve = V_east.Sum() / vectors.Count;
            double vn = V_north.Sum() / vectors.Count;

            double mean_WD = Math.Atan2(ve, vn) * 180 / Math.PI;
            mean_WD = (360 + mean_WD) % 360;

            return mean_WD;
        }
    }
}

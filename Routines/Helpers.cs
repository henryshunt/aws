﻿using System;
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

        public static double CalculateWindDirection(List<Vector> vectors)
        {
            List<double> east = new List<double>();
            List<double> north = new List<double>();

            foreach (Vector vector in vectors)
            {
                east.Add(vector.Magnitude * Math.Sin(vector.Direction * Math.PI / 180));
                north.Add(vector.Magnitude * Math.Cos(vector.Direction * Math.PI / 180));
            }

            double eastAverage = east.Sum() / vectors.Count;
            double northAverage = north.Sum() / vectors.Count;

            double direction = Math.Atan2(eastAverage, northAverage) * 180 / Math.PI;
            direction = (360 + direction) % 360;
            return direction;
        }

        public static double CalculateDewPoint(double temperature, double humidity)
        {
            double ea = (8.082 - temperature / 556.0) * temperature;
            double e = 0.4343 * Math.Log(humidity / 100) + ea / (256.1 + temperature);
            double sr = Math.Sqrt(Math.Pow(8.0813 - e, 2) - (1.842 * e));
            return 278.04 * (8.0813 - e - sr);
        }
    }
}

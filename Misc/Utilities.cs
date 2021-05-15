using System;
using System.Collections.Generic;
using System.Linq;

namespace Aws.Misc
{
    /// <summary>
    /// Provides various utility functions and constants.
    /// </summary>
    internal static class Utilities
    {
        /// <summary>
        /// The path to the file used for logging messages and exceptions.
        /// </summary>
        public const string LOGGING_FILE = "/var/logs/aws.log";

        /// <summary>
        /// The path to the configuration file.
        /// </summary>
        public const string CONFIG_FILE = "/etc/aws.json";

        /// <summary>
        /// The path to the directory where the program stores data.
        /// </summary>
        public const string DATA_DIRECTORY = "/var/aws/";

        /// <summary>
        /// Logs a message to the log file and outputs it to the console.
        /// </summary>
        /// <param name="message">
        /// The message to log.
        /// </param>
        public static void LogMessage(string message)
        {
            Console.WriteLine(message);
        }

        /// <summary>
        /// Logs an exception to the log file and outputs it to the console.
        /// </summary>
        /// <param name="message">
        /// The exception to log.
        /// </param>
        public static void LogException(Exception exception)
        {
            Console.WriteLine(exception);
        }

        /// <summary>
        /// Calculates the dew point for a given temperature and relative humidity.
        /// </summary>
        /// <param name="temperature">
        /// The temperature.
        /// </param>
        /// <param name="humidity">
        /// The relative humidity.
        /// </param>
        /// <returns>
        /// The calculated dew point.
        /// </returns>
        public static double CalculateDewPoint(double temperature, double humidity)
        {
            double ea = (8.082 - temperature / 556.0) * temperature;
            double e = 0.4343 * Math.Log(humidity / 100) + ea / (256.1 + temperature);
            double sr = Math.Sqrt(Math.Pow(8.0813 - e, 2) - (1.842 * e));
            return 278.04 * (8.0813 - e - sr);
        }

        /// <summary>
        /// Calculates the mean sea level pressure for a given pressure taken at station elevation.
        /// </summary>
        /// <param name="pressure">
        /// The pressure taken at station elevation.
        /// </param>
        /// <param name="temperature">
        /// The air temperature at the time the pressure measurement was taken.
        /// </param>
        /// <param name="elevation">
        /// The elevation that the pressure measurement was taken at.
        /// </param>
        /// <remarks>
        /// Uses the formula from <a href="https://keisan.casio.com/exec/system/1224575267">here</a>.
        /// </remarks>
        /// <returns>
        /// The calculated mean sea level pressure.
        /// </returns>
        public static double CalculateMeanSeaLevelPressure(double pressure, double temperature,
            double elevation)
        {
            double x = (0.0065 * elevation) /
                (temperature + (0.0065 * elevation) + 273.15);
            return pressure * Math.Pow(1 - x, -5.257);
        }

        /// <summary>
        /// Calculates the average direction of a list of vectors.
        /// </summary>
        /// <param name="vectors">
        /// The list of vectors.
        /// </param>
        /// <returns>
        /// The average direction of the list of vectors.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="vectors"/> is empty.
        /// </exception>
        public static double VectorDirectionAverage(List<Vector> vectors)
        {
            if (vectors.Count == 0)
                throw new ArgumentException(nameof(vectors) + " cannot be empty", nameof(vectors));

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
            return (360 + direction) % 360;
        }
    }
}

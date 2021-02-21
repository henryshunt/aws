using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.Linq.Expressions;
using static Aws.Routines.Helpers;

namespace Aws.Routines
{
    internal static class Database
    {
        private static string DATA_FILE = DATA_DIRECTORY + "data.sq3";
        private static string TRANSMIT_FILE = DATA_DIRECTORY + "transmit.sq3";

        public enum DatabaseFile { Data, Transmit };


        public static bool Exists(DatabaseFile database)
        {
            string file = database == DatabaseFile.Data ? DATA_FILE : TRANSMIT_FILE;
            return File.Exists(file);
        }

        public static SqliteConnection Connect(DatabaseFile database)
        {
            string file = database == DatabaseFile.Data ? DATA_FILE : TRANSMIT_FILE;
            return new SqliteConnection(string.Format("Data Source={0};", file));
        }


        public static void Create(DatabaseFile database)
        {
            string file = database == DatabaseFile.Data ? DATA_FILE : TRANSMIT_FILE;
            File.WriteAllBytes(file, new byte[0]);

            using (SqliteConnection connection = Connect(database))
            {
                connection.Open();
                string sql = "CREATE TABLE reports (time TEXT PRIMARY KEY, air_temperature REAL, " +
                    "relative_humidity REAL, dew_point REAL, wind_speed REAL, wind_direction INTEGER, " +
                    "wind_gust_speed REAL, wind_gust_direction INTEGER, rainfall REAL, station_pressure REAL, " +
                    "msl_pressure REAL, soil_temperature_10 REAL, soil_temperature_30 REAL, soil_temperature_100 REAL)";

                SqliteCommand query = new SqliteCommand(sql, connection);
                query.ExecuteNonQuery();
            }
        }

        public static void WriteReport(Report report)
        {
            using (SqliteConnection connection = Connect(DatabaseFile.Data))
            {
                connection.Open();
                string sql = "INSERT INTO reports (time, air_temperature, relative_humidity, dew_point, wind_speed, " +
                    "wind_direction, wind_gust_speed, rainfall, station_pressure, msl_pressure, " +
                    "soil_temperature_10, soil_temperature_30, soil_temperature_100) VALUES (@Time, @AirTemperature, " +
                    "@RelativeHumidity, @DewPoint, @WindSpeed, @WindDirection, @WindGustSpeed, " +
                    "@Rainfall, @StationPressure, @MSLPressure, @SoilTemperature10, @SoilTemperature30, " +
                    "@SoilTemperature100)";

                SqliteCommand query = new SqliteCommand(sql, connection);
                query.CommandText = sql;
                query.Connection = connection;

                query.Parameters.AddWithValue("@Time", report.Time.ToString("dd/MM/yyyy HH:mm:ss"));

                if (report.AirTemperature == null)
                    query.Parameters.AddWithValue("@AirTemperature", DBNull.Value);
                else query.Parameters.AddWithValue("@AirTemperature", report.AirTemperature);

                if (report.RelativeHumidity == null)
                    query.Parameters.AddWithValue("@RelativeHumidity", DBNull.Value);
                else query.Parameters.AddWithValue("@RelativeHumidity", report.RelativeHumidity);

                if (report.DewPoint == null)
                    query.Parameters.AddWithValue("@DewPoint", DBNull.Value);
                else query.Parameters.AddWithValue("@DewPoint", report.DewPoint);

                if (report.WindSpeed == null)
                    query.Parameters.AddWithValue("@WindSpeed", DBNull.Value);
                else query.Parameters.AddWithValue("@WindSpeed", report.WindSpeed);

                if (report.WindDirection == null)
                    query.Parameters.AddWithValue("@WindDirection", DBNull.Value);
                else query.Parameters.AddWithValue("@WindDirection", report.WindDirection);

                if (report.WindGust == null)
                    query.Parameters.AddWithValue("@WindGustSpeed", DBNull.Value);
                else query.Parameters.AddWithValue("@WindGustSpeed", report.WindGust);

                if (report.Rainfall == null)
                    query.Parameters.AddWithValue("@Rainfall", DBNull.Value);
                else query.Parameters.AddWithValue("@Rainfall", report.Rainfall);

                if (report.BarometricPressure == null)
                    query.Parameters.AddWithValue("@StationPressure", DBNull.Value);
                else query.Parameters.AddWithValue("@StationPressure", report.BarometricPressure);

                if (report.MslPressure == null)
                    query.Parameters.AddWithValue("@MSLPressure", DBNull.Value);
                else query.Parameters.AddWithValue("@MSLPressure", report.MslPressure);

                if (report.SoilTemperature10 == null)
                    query.Parameters.AddWithValue("@SoilTemperature10", DBNull.Value);
                else query.Parameters.AddWithValue("@SoilTemperature10", report.SoilTemperature10);

                if (report.SoilTemperature30 == null)
                    query.Parameters.AddWithValue("@SoilTemperature30", DBNull.Value);
                else query.Parameters.AddWithValue("@SoilTemperature30", report.SoilTemperature30);

                if (report.SoilTemperature100 == null)
                    query.Parameters.AddWithValue("@SoilTemperature100", DBNull.Value);
                else query.Parameters.AddWithValue("@SoilTemperature100", report.SoilTemperature100);

                query.ExecuteReader();
            }
        }
    }
}

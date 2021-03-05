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
                string sql = "CREATE TABLE reports (time TEXT PRIMARY KEY, " +
                    "airTemp REAL, relHum REAL, dewPoint REAL, windSpeed REAL, " +
                    "windDir INTEGER, windGust REAL, rainfall REAL, staPres REAL, " +
                    "mslPres REAL)";

                SqliteCommand query = new SqliteCommand(sql, connection);
                query.ExecuteNonQuery();
            }
        }

        public static void WriteReport(Report report)
        {
            using (SqliteConnection connection = Connect(DatabaseFile.Data))
            {
                connection.Open();
                string sql = "INSERT INTO reports VALUES (@Time, @AirTemperature, " +
                    "@RelativeHumidity, @DewPoint, @WindSpeed, @WindDirection, @WindGust, " +
                    "@Rainfall, @StationPressure, @MslPressure)";

                SqliteCommand query = new SqliteCommand(sql, connection);
                query.CommandText = sql;
                query.Connection = connection;

                query.Parameters.AddWithValue(
                    "@Time", report.Time.ToString("yyyy-MM-dd HH:mm:ss"));

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
                    query.Parameters.AddWithValue("@WindGust", DBNull.Value);
                else query.Parameters.AddWithValue("@WindGust", report.WindGust);

                if (report.Rainfall == null)
                    query.Parameters.AddWithValue("@Rainfall", DBNull.Value);
                else query.Parameters.AddWithValue("@Rainfall", report.Rainfall);

                if (report.BarometricPressure == null)
                    query.Parameters.AddWithValue("@StationPressure", DBNull.Value);
                else query.Parameters.AddWithValue("@StationPressure", report.BarometricPressure);

                if (report.MslPressure == null)
                    query.Parameters.AddWithValue("@MslPressure", DBNull.Value);
                else query.Parameters.AddWithValue("@MslPressure", report.MslPressure);

                query.ExecuteReader();
            }
        }
    }
}

using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using static Aws.Misc.Utilities;

namespace Aws.Misc
{
    /// <summary>
    /// Provides various database-related methods.
    /// </summary>
    public static class Database
    {
        /// <summary>
        /// The path to the SQLite database file that stores logged data.
        /// </summary>
        private static string DATA_FILE = DATA_DIRECTORY + "data.sq3";

        /// <summary>
        /// The path to the SQLite database file that stores data for transmission.
        /// </summary>
        private static string UPLOAD_FILE = DATA_DIRECTORY + "upload.sq3";

        /// <summary>
        /// Determines whether a database exists.
        /// </summary>
        /// <param name="database">
        /// The database to check.
        /// </param>
        /// <returns>
        /// <see cref="true"/> if the database exists, otherwise <see cref="false"/>.
        /// </returns>
        public static bool Exists(DatabaseFile database)
        {
            return File.Exists(database == DatabaseFile.Data ? DATA_FILE : UPLOAD_FILE);
        }

        /// <summary>
        /// Connects to a database.
        /// </summary>
        /// <param name="database">
        /// The database to connect to.
        /// </param>
        /// <returns>
        /// A connection to the database.
        /// </returns>
        public static SqliteConnection Connect(DatabaseFile database)
        {
            string path = database == DatabaseFile.Data ? DATA_FILE : UPLOAD_FILE;

            return new SqliteConnection(
                string.Format("Data Source={0};Mode=ReadWrite", path));
        }

        /// <summary>
        /// Creates a database.
        /// </summary>
        /// <param name="database">
        /// The database to create.
        /// </param>
        public static void Create(DatabaseFile database)
        {
            string path = database == DatabaseFile.Data ? DATA_FILE : UPLOAD_FILE;
            File.WriteAllBytes(path, new byte[0]);

            const string observationsSql = "CREATE TABLE observations (" +
                "time TEXT PRIMARY KEY NOT NULL, airTemp REAL, relHum REAL, dewPoint REAL, " +
                "windSpeed REAL, windDir INTEGER, windGust REAL, rainfall REAL, sunDur INTEGER, " +
                "staPres REAL, mslPres REAL)";

            string dayStatsSql = "CREATE TABLE dayStats(" +
                "date TEXT PRIMARY KEY NOT NULL{0}, airTempAvg REAL, airTempMin REAL, airTempMax REAL, " +
                "relHumAvg REAL, relHumMin REAL, relHumMax REAL, windSpeedAvg REAL, windSpeedMin REAL, " +
                "windSpeedMax REAL, windDirAvg INTEGER, windGustAvg REAL, windGustMin REAL, " +
                "windGustMax REAL, rainfallTtl REAL, sunDurTtl INTEGER, mslPresAvg REAL, " +
                "mslPresMin REAL, mslPresMax REAL)";

            // Random column is used to identify when records have changed
            if (database == DatabaseFile.Upload)
                dayStatsSql = string.Format(dayStatsSql, ", random INTEGER NOT NULL");
            else dayStatsSql = string.Format(dayStatsSql, "");

            using (SqliteConnection connection = Connect(database))
            {
                connection.Open();
                new SqliteCommand(observationsSql, connection).ExecuteNonQuery();
                new SqliteCommand(dayStatsSql, connection).ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Inserts an observation into a database.
        /// </summary>
        /// <param name="observation">
        /// The observation to insert.
        /// </param>
        /// <param name="database">
        /// The database to insert the observation into.
        /// </param>
        public static void WriteObservation(Observation observation, DatabaseFile database)
        {
            const string sql = "INSERT INTO observations VALUES (@time, @airTemp, @relHum, @dewPoint, " +
                "@windSpeed, @windDir, @windGust, @rainfall, @sunDur, @staPres, @mslPres)";

            using (SqliteConnection connection = Connect(database))
            {
                connection.Open();
                SqliteCommand query = new SqliteCommand(sql, connection);

                query.Parameters.AddWithValue("@time",
                    observation.Time.ToString("yyyy-MM-dd HH:mm:ss"));
                query.Parameters.AddWithValue("@airTemp",
                    observation.AirTemperature != null ? observation.AirTemperature : DBNull.Value);
                query.Parameters.AddWithValue("@relHum",
                    observation.RelativeHumidity != null ? observation.RelativeHumidity : DBNull.Value);
                query.Parameters.AddWithValue("@dewPoint",
                    observation.DewPoint != null ? observation.DewPoint : DBNull.Value);
                query.Parameters.AddWithValue("@windSpeed",
                    observation.WindSpeed != null ? observation.WindSpeed : DBNull.Value);
                query.Parameters.AddWithValue("@windDir",
                    observation.WindDirection != null ? observation.WindDirection : DBNull.Value);
                query.Parameters.AddWithValue("@windGust",
                    observation.WindGust != null ? observation.WindGust : DBNull.Value);
                query.Parameters.AddWithValue("@rainfall",
                    observation.Rainfall != null ? observation.Rainfall : DBNull.Value);
                query.Parameters.AddWithValue("@sunDur",
                    observation.SunshineDuration != null ? observation.SunshineDuration : DBNull.Value);
                query.Parameters.AddWithValue("@staPres",
                    observation.StationPressure != null ? observation.StationPressure : DBNull.Value);
                query.Parameters.AddWithValue("@mslPres",
                    observation.MslPressure != null ? observation.MslPressure : DBNull.Value);

                query.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Calculates various statistics over all records for a particular date in the local time zone.
        /// </summary>
        /// <param name="date">
        /// The date, in the local time zone, to calculate the statistics for.
        /// </param>
        /// <param name="timeZone">
        /// The local time zone.
        /// </param>
        /// <returns>
        /// The calculated statistics.
        /// </returns>
        public static DailyStatistic CalculateDailyStatistic(DateTime date, TimeZoneInfo timeZone)
        {
            DateTime start = new DateTime(date.Year, date.Month, date.Day, 0, 1, 0);
            DateTime end = start + TimeSpan.FromDays(1) - TimeSpan.FromMinutes(1);

            start = TimeZoneInfo.ConvertTimeToUtc(start, timeZone);
            end = TimeZoneInfo.ConvertTimeToUtc(end, timeZone);

            const string sql = "SELECT ROUND(AVG(airTemp), 1) AS airTempAvg, " +
                "MIN(airTemp) AS airTempMin, MAX(airTemp) AS airTempMax, " +
                "ROUND(AVG(relHum), 1) AS relHumAvg, " +
                "MIN(relHum) AS relHumMin, MAX(relHum) AS relHumMax, " +
                "ROUND(AVG(windSpeed), 1) AS windSpeedAvg, " +
                "MIN(windSpeed) AS windSpeedMin, MAX(windSpeed) AS windSpeedMax, " +
                "ROUND(AVG(windGust), 1) AS windGustAvg, " +
                "MIN(windGust) AS windGustMin, MAX(windGust) AS windGustMax, " +
                "SUM(rainfall) AS rainfallTtl, SUM(sunDur) AS sunDurTtl, " +
                "ROUND(AVG(mslPres), 1) AS mslPresAvg, " +
                "MIN(mslPres) AS mslPresMin, MAX(mslPres) AS mslPresMax " +
                "FROM observations WHERE time BETWEEN @start AND @end";

            using (SqliteConnection connection = Connect(DatabaseFile.Data))
            {
                connection.Open();

                SqliteCommand query = new SqliteCommand(sql, connection);
                query.Parameters.AddWithValue("@start", start.ToString("yyyy-MM-dd HH:mm:ss"));
                query.Parameters.AddWithValue("@end", end.ToString("yyyy-MM-dd HH:mm:ss"));

                using (SqliteDataReader reader = query.ExecuteReader())
                {
                    reader.Read();

                    return new DailyStatistic(date)
                    {
                        AirTemperatureAverage = !reader.IsDBNull(0) ? reader.GetDouble(0) : null,
                        AirTemperatureMinimum = !reader.IsDBNull(1) ? reader.GetDouble(1) : null,
                        AirTemperatureMaximum = !reader.IsDBNull(2) ? reader.GetDouble(2) : null,
                        RelativeHumidityAverage = !reader.IsDBNull(3) ? reader.GetDouble(3) : null,
                        RelativeHumidityMinimum = !reader.IsDBNull(4) ? reader.GetDouble(4) : null,
                        RelativeHumidityMaximum = !reader.IsDBNull(5) ? reader.GetDouble(5) : null,
                        WindSpeedAverage = !reader.IsDBNull(6) ? reader.GetDouble(6) : null,
                        WindSpeedMinimum = !reader.IsDBNull(7) ? reader.GetDouble(7) : null,
                        WindSpeedMaximum = !reader.IsDBNull(8) ? reader.GetDouble(8) : null,

                        // Need to manually calculate average wind direction because Microsoft.Data.Sqlite
                        // doesn't implement the required functions and I couldn't get System.Data.SQLite
                        // to work on the linux-arm platform
                        WindDirectionAverage = CalculateAverageWindDirection(start, end),

                        WindGustAverage = !reader.IsDBNull(9) ? reader.GetDouble(9) : null,
                        WindGustMinimum = !reader.IsDBNull(10) ? reader.GetDouble(10) : null,
                        WindGustMaximum = !reader.IsDBNull(11) ? reader.GetDouble(11) : null,
                        RainfallTotal = !reader.IsDBNull(12) ? reader.GetDouble(12) : null,
                        SunshineDurationTotal = !reader.IsDBNull(13) ? reader.GetInt32(13) : null,
                        MslPressureAverage = !reader.IsDBNull(14) ? reader.GetDouble(14) : null,
                        MslPressureMinimum = !reader.IsDBNull(15) ? reader.GetDouble(15) : null,
                        MslPressureMaximum = !reader.IsDBNull(16) ? reader.GetDouble(16) : null,
                    };
                }
            }
        }

        /// <summary>
        /// Calculates the average wind direction over all records within a time range.
        /// </summary>
        /// <param name="start">
        /// The start time of the range of records to use.
        /// </param>
        /// <param name="end">
        /// The end time of the range of records to use.
        /// </param>
        /// <returns>
        /// The average wind direction, or <see langword="null"/> if no records were found.
        /// </returns>
        private static int? CalculateAverageWindDirection(DateTime start, DateTime end)
        {
            const string sql = "SELECT time, windSpeed, windDir " +
                "FROM observations WHERE time BETWEEN @start AND @end";

            using (SqliteConnection connection = Connect(DatabaseFile.Data))
            {
                connection.Open();

                SqliteCommand query = new SqliteCommand(sql, connection);
                query.Parameters.AddWithValue("@start", start.ToString("yyyy-MM-dd HH:mm:ss"));
                query.Parameters.AddWithValue("@end", end.ToString("yyyy-MM-dd HH:mm:ss"));

                using (SqliteDataReader reader = query.ExecuteReader())
                {
                    List<Vector> vectors = new List<Vector>();

                    // Create a vector (speed and direction pair) for each record
                    while (reader.Read())
                    {
                        if (!reader.IsDBNull(1) && !reader.IsDBNull(2))
                            vectors.Add(new Vector(reader.GetDouble(1), reader.GetInt32(2)));
                    }

                    if (vectors.Count > 0)
                        return (int)(Math.Round(VectorDirectionAverage(vectors)) % 360);
                    else return null;
                }
            }
        }

        /// <summary>
        /// Inserts a daily statistic into a database. If a record with that date already exists then
        /// it is updated.
        /// </summary>
        /// <param name="statistic">
        /// The statistic to insert.
        /// </param>
        /// <param name="database">
        /// The database to insert the statistic into.
        /// </param>
        public static void WriteDailyStatistic(DailyStatistic statistic, DatabaseFile database)
        {
            string sql = "INSERT INTO dayStats VALUES (" +
                "@date{0}, @airTempAvg, @airTempMin, @airTempMax, @relHumAvg, @relHumMin, @relHumMax, " +
                "@windSpeedAvg, @windSpeedMin, @windSpeedMax, @windDirAvg, @windGustAvg, @windGustMin, " +
                "@windGustMax, @rainfallTtl, @sunDurTtl, @mslPresAvg, @mslPresMin, @mslPresMax) " +
                "ON CONFLICT (date) DO UPDATE SET " +
                "{1}airTempAvg = @airTempAvg, airTempMin = @airTempMin, airTempMax = @airTempMax, " +
                "relHumAvg = @relHumAvg, relHumMin = @relHumMin, relHumMax = @relHumMax, " +
                "windSpeedAvg = @windSpeedAvg, windSpeedMin = @windSpeedMin, windSpeedMax = @windSpeedMax, " +
                "windDirAvg = @windDirAvg, windGustAvg = @windGustAvg, windGustMin = @windGustMin, " +
                "windGustMax = @windGustMax, rainfallTtl = @rainfallTtl, sunDurTtl = @sunDurTtl, " +
                "mslPresAvg = @mslPresAvg, mslPresMin = @mslPresMin, mslPresMax = @mslPresMax";

            // Random column is used to identify when records have changed
            if (database == DatabaseFile.Upload)
                sql = string.Format(sql, ", @random", "random = @random, ");
            else sql = string.Format(sql, "", "");

            using (SqliteConnection connection = Connect(database))
            {
                connection.Open();

                SqliteCommand query = new SqliteCommand(sql, connection);
                query.Parameters.AddWithValue("@date", statistic.Date.ToString("yyyy-MM-dd"));

                if (database == DatabaseFile.Upload)
                    query.Parameters.AddWithValue("@random", new Random().Next());

                query.Parameters.AddWithValue("@airTempAvg", statistic.AirTemperatureAverage != null ?
                    statistic.AirTemperatureAverage : DBNull.Value);
                query.Parameters.AddWithValue("@airTempMin", statistic.AirTemperatureMinimum != null ?
                    statistic.AirTemperatureMinimum : DBNull.Value);
                query.Parameters.AddWithValue("@airTempMax", statistic.AirTemperatureMaximum != null ?
                    statistic.AirTemperatureMaximum : DBNull.Value);

                query.Parameters.AddWithValue("@relHumAvg", statistic.RelativeHumidityAverage != null ?
                    statistic.RelativeHumidityAverage : DBNull.Value);
                query.Parameters.AddWithValue("@relHumMin", statistic.RelativeHumidityMinimum != null ?
                    statistic.RelativeHumidityMinimum : DBNull.Value);
                query.Parameters.AddWithValue("@relHumMax", statistic.RelativeHumidityMaximum != null ?
                    statistic.RelativeHumidityMaximum : DBNull.Value);

                query.Parameters.AddWithValue("@windSpeedAvg", statistic.WindSpeedAverage != null ?
                    statistic.WindSpeedAverage : DBNull.Value);
                query.Parameters.AddWithValue("@windSpeedMin", statistic.WindSpeedMinimum != null ?
                    statistic.WindSpeedMinimum : DBNull.Value);
                query.Parameters.AddWithValue("@windSpeedMax", statistic.WindSpeedMaximum != null ?
                    statistic.WindSpeedMaximum : DBNull.Value);

                query.Parameters.AddWithValue("@windDirAvg", statistic.WindDirectionAverage != null ?
                    statistic.WindDirectionAverage : DBNull.Value);

                query.Parameters.AddWithValue("@windGustAvg", statistic.WindGustAverage != null ?
                    statistic.WindGustAverage : DBNull.Value);
                query.Parameters.AddWithValue("@windGustMin", statistic.WindGustMinimum != null ?
                    statistic.WindGustMinimum : DBNull.Value);
                query.Parameters.AddWithValue("@windGustMax", statistic.WindGustMaximum != null ?
                    statistic.WindGustMaximum : DBNull.Value);

                query.Parameters.AddWithValue("@rainfallTtl", statistic.RainfallTotal != null ?
                    statistic.RainfallTotal : DBNull.Value);
                query.Parameters.AddWithValue("@sunDurTtl", statistic.SunshineDurationTotal != null ?
                    statistic.SunshineDurationTotal : DBNull.Value);

                query.Parameters.AddWithValue("@mslPresAvg", statistic.MslPressureAverage != null ?
                    statistic.MslPressureAverage : DBNull.Value);
                query.Parameters.AddWithValue("@mslPresMin", statistic.MslPressureMinimum != null ?
                    statistic.MslPressureMinimum : DBNull.Value);
                query.Parameters.AddWithValue("@mslPresMax", statistic.MslPressureMaximum != null ?
                    statistic.MslPressureMaximum : DBNull.Value);

                query.ExecuteNonQuery();
            }
        }
    }
}

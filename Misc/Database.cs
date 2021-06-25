using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using static Aws.Misc.Utilities;

namespace Aws.Misc
{
    /// <summary>
    /// Provides various methods and constants for working with databases.
    /// </summary>
    internal static class Database
    {
        /// <summary>
        /// The path to the <see cref="DatabaseFile.Data"/> SQLite database.
        /// </summary>
        private static readonly string DATA_FILE = Path.Combine(DATA_DIRECTORY, "data.sqlite");

        /// <summary>
        /// The path to the <see cref="DatabaseFile.Upload"/> SQLite database.
        /// </summary>
        private static readonly string UPLOAD_FILE = Path.Combine(DATA_DIRECTORY, "upload.sqlite");

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
        /// The database connection.
        /// </returns>
        public static SqliteConnection Connect(DatabaseFile database)
        {
            string file = database == DatabaseFile.Data ? DATA_FILE : UPLOAD_FILE;

            return new SqliteConnection(
                string.Format("Data Source={0};Mode=ReadWrite", file));
        }

        /// <summary>
        /// Creates a database.
        /// </summary>
        /// <param name="database">
        /// The database to create.
        /// </param>
        public static void Create(DatabaseFile database)
        {
            string file = database == DatabaseFile.Data ? DATA_FILE : UPLOAD_FILE;
            File.WriteAllBytes(file, Array.Empty<byte>());

            const string observationsSql = "CREATE TABLE observations (" +
                "time TEXT PRIMARY KEY NOT NULL, airTemp REAL, relHum REAL, dewPoint REAL, " +
                "windSpeed REAL, windDir INTEGER, windGust REAL, rainfall REAL, sunDur INTEGER, " +
                "staPres REAL, mslPres REAL)";

            string dailyStatsSql = "CREATE TABLE dailyStats (" +
                "date TEXT PRIMARY KEY NOT NULL{0}, airTempAvg REAL, airTempMin REAL, airTempMax REAL, " +
                "relHumAvg REAL, relHumMin REAL, relHumMax REAL, dewPointAvg REAL, dewPointMin REAL, " +
                "dewPointMax REAL, windSpeedAvg REAL, windSpeedMin REAL, windSpeedMax REAL, " +
                "windDirAvg INTEGER, windGustAvg REAL, windGustMin REAL, windGustMax REAL, " +
                "rainfallTtl REAL, sunDurTtl INTEGER, mslPresAvg REAL, mslPresMin REAL, mslPresMax REAL)";

            string monthlyStatsSql = "CREATE TABLE monthlyStats (" +
                "year INTEGER NOT NULL, month INTEGER NOT NULL{0}, airTempAvg REAL, airTempMin REAL, " +
                "airTempMax REAL, relHumAvg REAL, relHumMin REAL, relHumMax REAL, dewPointAvg REAL, " +
                "dewPointMin REAL, dewPointMax REAL, windSpeedAvg REAL, windSpeedMin REAL, " +
                "windSpeedMax REAL, windDirAvg INTEGER, windGustAvg REAL, windGustMin REAL, " +
                "windGustMax REAL, rainfallTtl REAL, sunDurTtl INTEGER, mslPresAvg REAL, mslPresMin REAL, " +
                "mslPresMax REAL, PRIMARY KEY(year, month))";

            // Random column is used to identify when a record has been updated
            if (database == DatabaseFile.Upload)
            {
                dailyStatsSql = string.Format(dailyStatsSql, ", random INTEGER NOT NULL");
                monthlyStatsSql = string.Format(monthlyStatsSql, ", random INTEGER NOT NULL");
            }
            else
            {
                dailyStatsSql = string.Format(dailyStatsSql, "");
                monthlyStatsSql = string.Format(monthlyStatsSql, "");
            }

            using (SqliteConnection connection = Connect(database))
            {
                connection.Open();
                new SqliteCommand(observationsSql, connection).ExecuteNonQuery();
                new SqliteCommand(dailyStatsSql, connection).ExecuteNonQuery();
                new SqliteCommand(monthlyStatsSql, connection).ExecuteNonQuery();
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
        /// Calculates statistics for the observations logged over a day in the local time zone.
        /// </summary>
        /// <param name="date">
        /// The date, in the local time zone, of the day to calculate the statistics for.
        /// </param>
        /// <param name="timeZone">
        /// The local time zone.
        /// </param>
        public static DailyStatistics CalculateDailyStatistics(DateTime date,
            TimeZoneInfo timeZone)
        {
            DateTime start = new DateTime(date.Year, date.Month, date.Day, 0, 1, 0);
            DateTime end = start + new TimeSpan(23, 59, 0);

            start = TimeZoneInfo.ConvertTimeToUtc(start, timeZone);
            end = TimeZoneInfo.ConvertTimeToUtc(end, timeZone);

            const string sql = "SELECT ROUND(AVG(airTemp), 1), MIN(airTemp), MAX(airTemp), " +
                "ROUND(AVG(relHum), 1), MIN(relHum), MAX(relHum), ROUND(AVG(dewPoint), 1), MIN(dewPoint), " +
                "MAX(dewPoint), ROUND(AVG(windSpeed), 1), MIN(windSpeed), MAX(windSpeed), " +
                "ROUND(AVG(windGust), 1), MIN(windGust), MAX(windGust), SUM(rainfall), SUM(sunDur), " +
                "ROUND(AVG(mslPres), 1), MIN(mslPres), MAX(mslPres) " +
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

                    return new DailyStatistics(date)
                    {
                        AirTemperatureAverage = !reader.IsDBNull(0) ? reader.GetDouble(0) : null,
                        AirTemperatureMinimum = !reader.IsDBNull(1) ? reader.GetDouble(1) : null,
                        AirTemperatureMaximum = !reader.IsDBNull(2) ? reader.GetDouble(2) : null,
                        RelativeHumidityAverage = !reader.IsDBNull(3) ? reader.GetDouble(3) : null,
                        RelativeHumidityMinimum = !reader.IsDBNull(4) ? reader.GetDouble(4) : null,
                        RelativeHumidityMaximum = !reader.IsDBNull(5) ? reader.GetDouble(5) : null,
                        DewPointAverage = !reader.IsDBNull(6) ? reader.GetDouble(6) : null,
                        DewPointMinimum = !reader.IsDBNull(7) ? reader.GetDouble(7) : null,
                        DewPointMaximum = !reader.IsDBNull(8) ? reader.GetDouble(8) : null,
                        WindSpeedAverage = !reader.IsDBNull(9) ? reader.GetDouble(9) : null,
                        WindSpeedMinimum = !reader.IsDBNull(10) ? reader.GetDouble(10) : null,
                        WindSpeedMaximum = !reader.IsDBNull(11) ? reader.GetDouble(11) : null,

                        // Need to manually calculate average wind direction because Microsoft.Data.Sqlite
                        // doesn't implement the required functions and I couldn't get System.Data.SQLite
                        // to work on the linux-arm platform
                        WindDirectionAverage = CalculateAverageWindDirection(connection, start, end),

                        WindGustAverage = !reader.IsDBNull(12) ? reader.GetDouble(12) : null,
                        WindGustMinimum = !reader.IsDBNull(13) ? reader.GetDouble(13) : null,
                        WindGustMaximum = !reader.IsDBNull(14) ? reader.GetDouble(14) : null,
                        RainfallTotal = !reader.IsDBNull(15) ? reader.GetDouble(15) : null,
                        SunshineDurationTotal = !reader.IsDBNull(16) ? reader.GetInt32(16) : null,
                        MslPressureAverage = !reader.IsDBNull(17) ? reader.GetDouble(17) : null,
                        MslPressureMinimum = !reader.IsDBNull(18) ? reader.GetDouble(18) : null,
                        MslPressureMaximum = !reader.IsDBNull(19) ? reader.GetDouble(19) : null,
                    };
                }
            }
        }

        /// <summary>
        /// Calculates statistics for the observations logged over a month in the local time zone.
        /// </summary>
        /// <param name="year">
        /// The year that the month is part of.
        /// </param>
        /// <param name="month">
        /// The month (1 through 12) to calculate the statistics for.
        /// </param>
        /// <param name="timeZone">
        /// The local time zone.
        /// </param>
        public static MonthlyStatistics CalculateMonthlyStatistics(int year, int month,
            TimeZoneInfo timeZone)
        {
            DateTime start = new DateTime(year, month, 1, 0, 1, 0);
            DateTime end = start.AddMonths(1) - TimeSpan.FromMinutes(1);

            start = TimeZoneInfo.ConvertTimeToUtc(start, timeZone);
            end = TimeZoneInfo.ConvertTimeToUtc(end, timeZone);

            const string sql = "SELECT ROUND(AVG(airTemp), 1), MIN(airTemp), MAX(airTemp), " +
                "ROUND(AVG(relHum), 1), MIN(relHum), MAX(relHum), ROUND(AVG(dewPoint), 1), MIN(dewPoint), " +
                "MAX(dewPoint), ROUND(AVG(windSpeed), 1), MIN(windSpeed), MAX(windSpeed), " +
                "ROUND(AVG(windGust), 1), MIN(windGust), MAX(windGust), SUM(rainfall), SUM(sunDur), " +
                "ROUND(AVG(mslPres), 1), MIN(mslPres), MAX(mslPres) " +
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

                    return new MonthlyStatistics(year, month)
                    {
                        AirTemperatureAverage = !reader.IsDBNull(0) ? reader.GetDouble(0) : null,
                        AirTemperatureMinimum = !reader.IsDBNull(1) ? reader.GetDouble(1) : null,
                        AirTemperatureMaximum = !reader.IsDBNull(2) ? reader.GetDouble(2) : null,
                        RelativeHumidityAverage = !reader.IsDBNull(3) ? reader.GetDouble(3) : null,
                        RelativeHumidityMinimum = !reader.IsDBNull(4) ? reader.GetDouble(4) : null,
                        RelativeHumidityMaximum = !reader.IsDBNull(5) ? reader.GetDouble(5) : null,
                        DewPointAverage = !reader.IsDBNull(6) ? reader.GetDouble(6) : null,
                        DewPointMinimum = !reader.IsDBNull(7) ? reader.GetDouble(7) : null,
                        DewPointMaximum = !reader.IsDBNull(8) ? reader.GetDouble(8) : null,
                        WindSpeedAverage = !reader.IsDBNull(9) ? reader.GetDouble(9) : null,
                        WindSpeedMinimum = !reader.IsDBNull(10) ? reader.GetDouble(10) : null,
                        WindSpeedMaximum = !reader.IsDBNull(11) ? reader.GetDouble(11) : null,

                        // Need to manually calculate average wind direction because Microsoft.Data.Sqlite
                        // doesn't implement the required functions and I couldn't get System.Data.SQLite
                        // to work on the linux-arm platform
                        WindDirectionAverage = CalculateAverageWindDirection(connection, start, end),

                        WindGustAverage = !reader.IsDBNull(12) ? reader.GetDouble(12) : null,
                        WindGustMinimum = !reader.IsDBNull(13) ? reader.GetDouble(13) : null,
                        WindGustMaximum = !reader.IsDBNull(14) ? reader.GetDouble(14) : null,
                        RainfallTotal = !reader.IsDBNull(15) ? reader.GetDouble(15) : null,
                        SunshineDurationTotal = !reader.IsDBNull(16) ? reader.GetInt32(16) : null,
                        MslPressureAverage = !reader.IsDBNull(17) ? reader.GetDouble(17) : null,
                        MslPressureMinimum = !reader.IsDBNull(18) ? reader.GetDouble(18) : null,
                        MslPressureMaximum = !reader.IsDBNull(19) ? reader.GetDouble(19) : null,
                    };
                }
            }
        }

        /// <summary>
        /// Calculates the average wind direction for the observations within a time range.
        /// </summary>
        /// <param name="connection">
        /// The database connection to use.
        /// </param>
        /// <param name="start">
        /// The start time of the range of observations to use, in UTC.
        /// </param>
        /// <param name="end">
        /// The end time of the range of observations to use, in UTC.
        /// </param>
        /// <returns>
        /// The average wind direction, or <see langword="null"/> if no observations were found.
        /// </returns>
        private static int? CalculateAverageWindDirection(SqliteConnection connection,
            DateTime start, DateTime end)
        {
            const string sql = "SELECT time, windSpeed, windDir " +
                "FROM observations WHERE time BETWEEN @start AND @end";

            SqliteCommand query = new SqliteCommand(sql, connection);
            query.Parameters.AddWithValue("@start", start.ToString("yyyy-MM-dd HH:mm:ss"));
            query.Parameters.AddWithValue("@end", end.ToString("yyyy-MM-dd HH:mm:ss"));

            using (SqliteDataReader reader = query.ExecuteReader())
            {
                List<Vector> vectors = new List<Vector>();

                // Create a vector (speed and direction pair) for each observation
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

        /// <summary>
        /// Inserts daily statistics into a database. If a record with that date already exists then it is updated.
        /// </summary>
        /// <param name="statistics">
        /// The daily statistics to insert.
        /// </param>
        /// <param name="database">
        /// The database to insert the daily statistics into.
        /// </param>
        public static void WriteDailyStatistics(DailyStatistics statistics, DatabaseFile database)
        {
            string sql = "INSERT INTO dailyStats VALUES (" +
                "@date{0}, @airTempAvg, @airTempMin, @airTempMax, @relHumAvg, @relHumMin, @relHumMax, " +
                "@dewPointAvg, @dewPointMin, @dewPointMax, @windSpeedAvg, @windSpeedMin, @windSpeedMax, " +
                "@windDirAvg, @windGustAvg, @windGustMin, @windGustMax, @rainfallTtl, @sunDurTtl, " +
                "@mslPresAvg, @mslPresMin, @mslPresMax) " +
                "ON CONFLICT (date) DO UPDATE SET " +
                "{1}airTempAvg = @airTempAvg, airTempMin = @airTempMin, airTempMax = @airTempMax, " +
                "dewPointAvg = @dewPointAvg, dewPointMin = @dewPointMin, dewPointMax = @dewPointMax, " +
                "relHumAvg = @relHumAvg, relHumMin = @relHumMin, relHumMax = @relHumMax, " +
                "windSpeedAvg = @windSpeedAvg, windSpeedMin = @windSpeedMin, windSpeedMax = @windSpeedMax, " +
                "windDirAvg = @windDirAvg, windGustAvg = @windGustAvg, windGustMin = @windGustMin, " +
                "windGustMax = @windGustMax, rainfallTtl = @rainfallTtl, sunDurTtl = @sunDurTtl, " +
                "mslPresAvg = @mslPresAvg, mslPresMin = @mslPresMin, mslPresMax = @mslPresMax";

            // Random column is used to identify when a record has been updated
            if (database == DatabaseFile.Upload)
                sql = string.Format(sql, ", @random", "random = @random, ");
            else sql = string.Format(sql, "", "");

            using (SqliteConnection connection = Connect(database))
            {
                connection.Open();

                SqliteCommand query = new SqliteCommand(sql, connection);
                query.Parameters.AddWithValue("@date", statistics.Date.ToString("yyyy-MM-dd"));

                if (database == DatabaseFile.Upload)
                    query.Parameters.AddWithValue("@random", new Random().Next());

                query.Parameters.AddWithValue("@airTempAvg", statistics.AirTemperatureAverage != null ?
                    statistics.AirTemperatureAverage : DBNull.Value);
                query.Parameters.AddWithValue("@airTempMin", statistics.AirTemperatureMinimum != null ?
                    statistics.AirTemperatureMinimum : DBNull.Value);
                query.Parameters.AddWithValue("@airTempMax", statistics.AirTemperatureMaximum != null ?
                    statistics.AirTemperatureMaximum : DBNull.Value);

                query.Parameters.AddWithValue("@relHumAvg", statistics.RelativeHumidityAverage != null ?
                    statistics.RelativeHumidityAverage : DBNull.Value);
                query.Parameters.AddWithValue("@relHumMin", statistics.RelativeHumidityMinimum != null ?
                    statistics.RelativeHumidityMinimum : DBNull.Value);
                query.Parameters.AddWithValue("@relHumMax", statistics.RelativeHumidityMaximum != null ?
                    statistics.RelativeHumidityMaximum : DBNull.Value);

                query.Parameters.AddWithValue("@dewPointAvg", statistics.DewPointAverage != null ?
                    statistics.DewPointAverage : DBNull.Value);
                query.Parameters.AddWithValue("@dewPointMin", statistics.DewPointMinimum != null ?
                    statistics.DewPointMinimum : DBNull.Value);
                query.Parameters.AddWithValue("@dewPointMax", statistics.DewPointMaximum != null ?
                    statistics.DewPointMaximum : DBNull.Value);

                query.Parameters.AddWithValue("@windSpeedAvg", statistics.WindSpeedAverage != null ?
                    statistics.WindSpeedAverage : DBNull.Value);
                query.Parameters.AddWithValue("@windSpeedMin", statistics.WindSpeedMinimum != null ?
                    statistics.WindSpeedMinimum : DBNull.Value);
                query.Parameters.AddWithValue("@windSpeedMax", statistics.WindSpeedMaximum != null ?
                    statistics.WindSpeedMaximum : DBNull.Value);

                query.Parameters.AddWithValue("@windDirAvg", statistics.WindDirectionAverage != null ?
                    statistics.WindDirectionAverage : DBNull.Value);

                query.Parameters.AddWithValue("@windGustAvg", statistics.WindGustAverage != null ?
                    statistics.WindGustAverage : DBNull.Value);
                query.Parameters.AddWithValue("@windGustMin", statistics.WindGustMinimum != null ?
                    statistics.WindGustMinimum : DBNull.Value);
                query.Parameters.AddWithValue("@windGustMax", statistics.WindGustMaximum != null ?
                    statistics.WindGustMaximum : DBNull.Value);

                query.Parameters.AddWithValue("@rainfallTtl", statistics.RainfallTotal != null ?
                    statistics.RainfallTotal : DBNull.Value);
                query.Parameters.AddWithValue("@sunDurTtl", statistics.SunshineDurationTotal != null ?
                    statistics.SunshineDurationTotal : DBNull.Value);

                query.Parameters.AddWithValue("@mslPresAvg", statistics.MslPressureAverage != null ?
                    statistics.MslPressureAverage : DBNull.Value);
                query.Parameters.AddWithValue("@mslPresMin", statistics.MslPressureMinimum != null ?
                    statistics.MslPressureMinimum : DBNull.Value);
                query.Parameters.AddWithValue("@mslPresMax", statistics.MslPressureMaximum != null ?
                    statistics.MslPressureMaximum : DBNull.Value);

                query.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Inserts monthly statistics into a database. If a record with that year and month already exists then it is
        /// updated.
        /// </summary>
        /// <param name="statistics">
        /// The monthly statistics to insert.
        /// </param>
        /// <param name="database">
        /// The database to insert the monthly statistics into.
        /// </param>
        public static void WriteMonthlyStatistics(MonthlyStatistics statistics,
            DatabaseFile database)
        {
            string sql = "INSERT INTO monthlyStats VALUES (" +
                "@year, @month{0}, @airTempAvg, @airTempMin, @airTempMax, @relHumAvg, @relHumMin, " +
                "@relHumMax, @dewPointAvg, @dewPointMin, @dewPointMax, @windSpeedAvg, @windSpeedMin, " +
                "@windSpeedMax, @windDirAvg, @windGustAvg, @windGustMin, @windGustMax, @rainfallTtl, " +
                "@sunDurTtl, @mslPresAvg, @mslPresMin, @mslPresMax) " +
                "ON CONFLICT (year, month) DO UPDATE SET " +
                "{1}airTempAvg = @airTempAvg, airTempMin = @airTempMin, airTempMax = @airTempMax, " +
                "dewPointAvg = @dewPointAvg, dewPointMin = @dewPointMin, dewPointMax = @dewPointMax, " +
                "relHumAvg = @relHumAvg, relHumMin = @relHumMin, relHumMax = @relHumMax, " +
                "windSpeedAvg = @windSpeedAvg, windSpeedMin = @windSpeedMin, windSpeedMax = @windSpeedMax, " +
                "windDirAvg = @windDirAvg, windGustAvg = @windGustAvg, windGustMin = @windGustMin, " +
                "windGustMax = @windGustMax, rainfallTtl = @rainfallTtl, sunDurTtl = @sunDurTtl, " +
                "mslPresAvg = @mslPresAvg, mslPresMin = @mslPresMin, mslPresMax = @mslPresMax";

            // Random column is used to identify when a record has been updated
            if (database == DatabaseFile.Upload)
                sql = string.Format(sql, ", @random", "random = @random, ");
            else sql = string.Format(sql, "", "");

            using (SqliteConnection connection = Connect(database))
            {
                connection.Open();

                SqliteCommand query = new SqliteCommand(sql, connection);
                query.Parameters.AddWithValue("@year", statistics.Year);
                query.Parameters.AddWithValue("@month", statistics.Month);

                if (database == DatabaseFile.Upload)
                    query.Parameters.AddWithValue("@random", new Random().Next());

                query.Parameters.AddWithValue("@airTempAvg", statistics.AirTemperatureAverage != null ?
                    statistics.AirTemperatureAverage : DBNull.Value);
                query.Parameters.AddWithValue("@airTempMin", statistics.AirTemperatureMinimum != null ?
                    statistics.AirTemperatureMinimum : DBNull.Value);
                query.Parameters.AddWithValue("@airTempMax", statistics.AirTemperatureMaximum != null ?
                    statistics.AirTemperatureMaximum : DBNull.Value);

                query.Parameters.AddWithValue("@relHumAvg", statistics.RelativeHumidityAverage != null ?
                    statistics.RelativeHumidityAverage : DBNull.Value);
                query.Parameters.AddWithValue("@relHumMin", statistics.RelativeHumidityMinimum != null ?
                    statistics.RelativeHumidityMinimum : DBNull.Value);
                query.Parameters.AddWithValue("@relHumMax", statistics.RelativeHumidityMaximum != null ?
                    statistics.RelativeHumidityMaximum : DBNull.Value);

                query.Parameters.AddWithValue("@dewPointAvg", statistics.DewPointAverage != null ?
                    statistics.DewPointAverage : DBNull.Value);
                query.Parameters.AddWithValue("@dewPointMin", statistics.DewPointMinimum != null ?
                    statistics.DewPointMinimum : DBNull.Value);
                query.Parameters.AddWithValue("@dewPointMax", statistics.DewPointMaximum != null ?
                    statistics.DewPointMaximum : DBNull.Value);

                query.Parameters.AddWithValue("@windSpeedAvg", statistics.WindSpeedAverage != null ?
                    statistics.WindSpeedAverage : DBNull.Value);
                query.Parameters.AddWithValue("@windSpeedMin", statistics.WindSpeedMinimum != null ?
                    statistics.WindSpeedMinimum : DBNull.Value);
                query.Parameters.AddWithValue("@windSpeedMax", statistics.WindSpeedMaximum != null ?
                    statistics.WindSpeedMaximum : DBNull.Value);

                query.Parameters.AddWithValue("@windDirAvg", statistics.WindDirectionAverage != null ?
                    statistics.WindDirectionAverage : DBNull.Value);

                query.Parameters.AddWithValue("@windGustAvg", statistics.WindGustAverage != null ?
                    statistics.WindGustAverage : DBNull.Value);
                query.Parameters.AddWithValue("@windGustMin", statistics.WindGustMinimum != null ?
                    statistics.WindGustMinimum : DBNull.Value);
                query.Parameters.AddWithValue("@windGustMax", statistics.WindGustMaximum != null ?
                    statistics.WindGustMaximum : DBNull.Value);

                query.Parameters.AddWithValue("@rainfallTtl", statistics.RainfallTotal != null ?
                    statistics.RainfallTotal : DBNull.Value);
                query.Parameters.AddWithValue("@sunDurTtl", statistics.SunshineDurationTotal != null ?
                    statistics.SunshineDurationTotal : DBNull.Value);

                query.Parameters.AddWithValue("@mslPresAvg", statistics.MslPressureAverage != null ?
                    statistics.MslPressureAverage : DBNull.Value);
                query.Parameters.AddWithValue("@mslPresMin", statistics.MslPressureMinimum != null ?
                    statistics.MslPressureMinimum : DBNull.Value);
                query.Parameters.AddWithValue("@mslPresMax", statistics.MslPressureMaximum != null ?
                    statistics.MslPressureMaximum : DBNull.Value);

                query.ExecuteNonQuery();
            }
        }
    }
}

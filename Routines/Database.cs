using Microsoft.Data.Sqlite;
using System.IO;

namespace AWS.Routines
{
    internal static class Database
    {
        private static string DATA_FILE = Helpers.DATA_DIRECTORY + "data.sq3";
        private static string TRANSMIT_FILE = Helpers.DATA_DIRECTORY + "transmit.sq3";

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
                string sql = "CREATE TABLE reports (time TEXT,air_temperature REAL,relative_humidity REAL,rainfall REAL)";
                SqliteCommand command = new SqliteCommand(sql, connection);
                command.ExecuteNonQuery();
            }
        }

        public static void WriteReport(Helpers.Report report)
        {

        }
    }
}

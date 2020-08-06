using AWS.Routines;
using NLog;
using NLog.Layouts;
using NLog.Targets;

namespace AWS
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ConfigureLogging();

            new Core.Coordinator().Startup();
        }

        private static void ConfigureLogging()
        {
            var loggingConfig = new NLog.Config.LoggingConfiguration();

            string loggingLayout = "${level:uppercase=true} | ${logger} -- ${message}";

            FileTarget loggingFile = new FileTarget("logfile");
            loggingFile.FileName = Helpers.LOGGING_FILE;
            loggingFile.Layout = Layout.FromString(loggingLayout);
            loggingConfig.AddRule(LogLevel.Debug, LogLevel.Fatal, loggingFile);

            ConsoleTarget loggingConsole = new ConsoleTarget("logconsole");
            loggingConsole.Layout = Layout.FromString(loggingLayout);
            loggingConfig.AddRule(LogLevel.Info, LogLevel.Fatal, loggingConsole);

            LogManager.Configuration = loggingConfig;
        }
    }
}

using AWS.Routines;
using NLog;
using NLog.Layouts;
using NLog.Targets;
using System;

namespace AWS
{
    internal class Program
    {
        private static readonly Logger eventLogger = LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            ConfigureLogging();

            new Core.Coordinator().Startup();

            // Log any (unexpected) uncaught exceptions in the program
            AppDomain.CurrentDomain.FirstChanceException +=
                (sender, e) => eventLogger.Error(e.Exception, "Uncaught exception");

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

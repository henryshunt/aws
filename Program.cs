using Aws.Routines;
using NLog;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;
using System;

namespace Aws
{
    internal class Program
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            ConfigureLogging();

            // Log any uncaught exceptions anywhere in the program
            AppDomain.CurrentDomain.FirstChanceException +=
                (sender, e) => logger.Error(e.Exception, "Uncaught exception");

            new Core.Coordinator().Startup();
        }

        private static void ConfigureLogging()
        {
            LoggingConfiguration loggingConfig = new LoggingConfiguration();

            string loggingLayout = "${level:uppercase=true} | ${logger} -- ${message}" +
                "${onexception:inner=${newline}${exception:format=tostring}}";

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

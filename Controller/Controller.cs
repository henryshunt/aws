using System;
using System.IO;
using static AWS.Helpers.Helpers;

namespace AWS.Controller
{
    internal class Controller
    {
        private DateTime StartupTime;
        private Helpers.Configuration Configuration = new Helpers.Configuration();
        private SchedulingClock SchedulingClock;

        private Subsystems.Logger LoggerSubsystem = new Subsystems.Logger();
        private Subsystems.Transmitter TransmitterSubsystem = new Subsystems.Transmitter();

        public void StartupProcedure()
        {
            StartupTime = DateTime.UtcNow;
            LogEvent(LoggingSource.Controller, "Startup procedure started");

            if (!Configuration.Load(CONFIG_FILE))
            {
                LogEvent(LoggingSource.Controller, "Error while loading the configuration file");
                return;
            }
            else LogEvent(LoggingSource.Controller, "Configuration file successfully loaded");

            try { Directory.CreateDirectory(DATA_DIRECTORY); }
            catch
            {
                LogEvent(LoggingSource.Controller, "Error while creating the data directory");
                return;
            }

            SchedulingClock = new SchedulingClock(Configuration);
            SchedulingClock.Start();

            LoggerSubsystem.Start(Configuration);
            TransmitterSubsystem.Start(Configuration);
        }
    }
}

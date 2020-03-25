using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Unosquare.RaspberryIO;
using Unosquare.WiringPi;
using static AWS.Routines.Helpers;

namespace AWS.Controller
{
    internal class Controller
    {
        private DateTime StartupTime;
        private Routines.Configuration Configuration = new Routines.Configuration();

        private Subsystems.Logger LoggerSubsystem = new Subsystems.Logger();
        private Subsystems.Transmitter TransmitterSubsystem = new Subsystems.Transmitter();

        private ExitAction ExitAction = ExitAction.None;

        public void StartupProcedure()
        {
            StartupTime = DateTime.UtcNow;
            LogEvent("Controller", "Startup procedure started");

            if (!Configuration.Load("/etc/aws.ini"))
            {
                LogEvent("Controller", "Error while loading the configuration file");
                return;
            }

            try { Pi.Init<BootstrapWiringPi>(); }
            catch
            {
                LogEvent("Controller", "Error while loading the GPIO implementation");
            }

            if (!Directory.Exists("/var/lib/aws"))
                Directory.CreateDirectory("/var/lib/aws");

            LoggerSubsystem.Start(Configuration);
            TransmitterSubsystem.Start(Configuration);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using static AWS.Helpers.Helpers;

namespace AWS.Subsystems
{
    internal class Transmitter : Subsystem
    {
        public override void SubsystemProcedure()
        {
            LogEvent(LoggingSource.Transmitter, "Subsystem procedure started");
        }
    }
}

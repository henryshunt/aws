using System;
using System.Collections.Generic;
using System.Text;
using static AWS.Routines.Helpers;

namespace AWS.Subsystems
{
    internal class Transmitter : Subsystem
    {
        public override void SubsystemProcedure()
        {
            LogEvent("Transmitter", "Subsystem procedure started");
        }
    }
}

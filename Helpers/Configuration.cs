using System;
using System.Collections.Generic;
using System.Text;

namespace AWS.Helpers
{
    internal class Configuration
    {
        public int SchedulingClockPin { get; private set; } = 14;

        public bool Load(string filePath)
        {
            return true;
        }

        private bool Validate()
        {
            return true;
        }
    }
}

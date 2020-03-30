﻿using AWS.Helpers;
using System;
using System.Device.Gpio;
using System.Threading;

namespace AWS.Subsystems
{
    internal class Subsystem
    {
        protected Configuration Configuration;
        private Thread SubsystemThread;
        protected bool ShouldReceiveSchedulingClockTicks = false;

        public void Start(Configuration configuration)
        {
            Configuration = configuration;
            SubsystemThread = new Thread(() => SubsystemProcedure());
            SubsystemThread.Start();
        }

        public virtual void SubsystemProcedure() { }

        protected void StartSchedulingClock()
        {
            using (GpioController controller = new GpioController())
            {
                controller.OpenPin(Configuration.SchedulingClockPin);
                controller.SetPinMode(Configuration.SchedulingClockPin, PinMode.Input);
                controller.RegisterCallbackForPinValueChangedEvent(
                    Configuration.SchedulingClockPin, PinEventTypes.Falling, OnSQWInterrupt);
            }
        }

        private void OnSQWInterrupt(object sender, PinValueChangedEventArgs pinValueChangedEventArgs)
        {
            ThreadPool.QueueUserWorkItem(a => OnSchedulingClockTick());
        }

        public virtual void OnSchedulingClockTick() { }
    }
}

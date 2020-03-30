using AWS.Hardware.Sensors;
using System;
using System.Threading;
using static AWS.Helpers.Helpers;

namespace AWS.Subsystems
{
    internal class Logger : Subsystem
    {
        private object SamplingTimerLock = new object();
        private bool HasStartedSampling = false;

        private bool IsFirstTimerInterrupt = true;

        private bool IsFirstSample = true;
        private bool IsFirstLog = true;

        private RR111 RainSensor = new Hardware.Sensors.RR111();


        public override void SubsystemProcedure()
        {
            LogEvent(LoggingSource.Logger, "Subsystem procedure started");

            // Monitor the scheduler pin for once-per-second interrupts
            //IGpioPin pin = Pi.Gpio[14];
            //pin.PinMode = GpioPinDriveMode.Input;
            //pin.RegisterInterruptCallback(EdgeDetection.FallingEdge, OnSchedulingInterrupt);

            RainSensor.Setup(4);

            StartSchedulingClock();
            while (true) ;
        }

        public override void OnSchedulingClockTick()
        {
            DateTime now = DateTime.UtcNow;

            if (HasStartedSampling)
            {
                SamplingProcedure();
                if (now.Second == 0)
                    LoggingProcedure();
            }
            else
            {
                if (now.Second == 0)
                    HasStartedSampling = true;
            }
        }

        private void SamplingProcedure()
        {
            DateTime now = DateTime.UtcNow;

            bool isFirstTimerInterrupt = IsFirstTimerInterrupt;
            if (isFirstTimerInterrupt)
            {
                IsFirstTimerInterrupt = false;
                RainSensor.IsPaused = false;
            }

            //if (now.Millisecond >= 0 && now.Millisecond <= 100)
            //    Console.WriteLine(DateTime.UtcNow.ToString("HH:mm:ss.fffff"));
            //else Console.WriteLine("slow");

            if (Monitor.TryEnter(SamplingTimerLock))
            {
                // Sample sensors
                Monitor.Exit(SamplingTimerLock);
            }

            if (now.Second == 0 && !isFirstTimerInterrupt)
                LoggingProcedure();
        }

        private void LoggingProcedure()
        {
            Console.WriteLine("Logging procedure");

            // Rain sensor
            SamplingBucket bucket = RainSensor.SamplingBucket;
            RainSensor.SwitchSamplingBucket();
            Console.WriteLine("Rainfall: " + RainSensor.CalculateTotal(bucket) + " mm");
            RainSensor.ResetSamplingBucket(bucket);
        }
    }
}

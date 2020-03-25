using AWS.Hardware.Sensors;
using System;
using System.Threading;
using static AWS.Routines.Helpers;

namespace AWS.Subsystems
{
    internal class Logger : Subsystem
    {
        private Timer SamplingTimer;
        private object SamplingTimerLock = new object();
        private bool IsFirstTimerInterrupt = true;

        private RR111 RainSensor = new Hardware.Sensors.RR111();

        public override void SubsystemProcedure()
        {
            LogEvent("Logger", "Subsystem procedure started");

            DateTime now = DateTime.UtcNow;
            DateTime nextMinute = RoundUp(now, TimeSpan.FromMinutes(1));
            int timerTick = Convert.ToInt32((nextMinute - now).TotalMilliseconds);
            SamplingTimer = new Timer(SamplingTimerInterrupt, null, timerTick, Timeout.Infinite);

            RainSensor.Setup(4);
            Console.ReadKey();
        }

        private void SamplingTimerInterrupt(object state)
        {
            DateTime now = DateTime.UtcNow;
            DateTime nextSecond = RoundUp(now, TimeSpan.FromSeconds(1));
            int timerTick = Convert.ToInt32((nextSecond - now).TotalMilliseconds);
            SamplingTimer.Change(timerTick, Timeout.Infinite);

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


        DateTime RoundUp(DateTime dt, TimeSpan d)
        {
            return new DateTime((dt.Ticks + d.Ticks - 1) / d.Ticks * d.Ticks, dt.Kind);
        }
    }
}

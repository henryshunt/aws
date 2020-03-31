using AWS.Routines;
using Iot.Device.Rtc;
using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Device.I2c;

namespace AWS.Routines
{
    internal class SchedulingClock
    {
        private Configuration Configuration;
        private Ds3231 RTC;

        public event EventHandler<SchedulingClockTickedEventArgs> Ticked;

        public SchedulingClock(Configuration configuration)
        {
            Configuration = configuration;
            RTC = new Ds3231(I2cDevice.Create(new I2cConnectionSettings(1, Ds3231.DefaultI2cAddress)));
            Console.WriteLine(RTC.DateTime);
        }

        public void Start()
        {
            // RTC.SetAlarmOne(0, 0, 0, 0, Hardware.DS3231.AlarmOneMode.OncePerSecond);

            // Monitor the scheduling clock pin for interrupts from the RTC
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
            // RTC.LatchAlarmsTriggeredFlags(); // Required to allow the alarm to trigger again
            Ticked?.Invoke(sender, new SchedulingClockTickedEventArgs(DateTime.UtcNow));
        }
    }
}

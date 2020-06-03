using Iot.Device.Rtc;
using System;
using System.Device.Gpio;
using System.Device.I2c;

namespace AWS.Routines
{
    internal class Clock
    {
        private Configuration Configuration;
        private Ds3231 RTC;

        public DateTime DateTime { get => RTC.DateTime; }

        public event EventHandler<ClockTickedEventArgs> Ticked;


        public Clock(Configuration configuration)
        {
            Configuration = configuration;
            RTC = new Ds3231(I2cDevice.Create(new I2cConnectionSettings(1, Ds3231.DefaultI2cAddress)));
        }

        public void Start()
        {
            RTC.Alarm1 = new Ds3231Alarm1(0, 0, 0, 0, Ds3231Alarm1.AlarmMatchMode.OncePerSecond);
            RTC.EnableAlarm(Ds3231.Alarm.Alarm1);

            // Monitor the scheduling clock pin for interrupts from the RTC
            GpioController controller = new GpioController();
            controller.OpenPin(Configuration.SchedulingClockPin);
            controller.SetPinMode(Configuration.SchedulingClockPin, PinMode.Input);
            controller.RegisterCallbackForPinValueChangedEvent(
                Configuration.SchedulingClockPin, PinEventTypes.Falling, OnPinInterrupt);
        }

        private void OnPinInterrupt(object sender, PinValueChangedEventArgs pinValueChangedEventArgs)
        {
            RTC.ResetAlarmState(Ds3231.Alarm.Alarm1);
            Ticked?.Invoke(sender, new ClockTickedEventArgs(RTC.DateTime));
        }
    }
}

using Iot.Device.Rtc;
using System;
using System.Device.Gpio;
using System.Device.I2c;

namespace AWS.Routines
{
    internal class Clock
    {
        private int tickInterruptPin;
        private Ds3231 rtc;

        public DateTime DateTime { get => rtc.DateTime; }

        public event EventHandler<ClockTickedEventArgs> Ticked;


        public Clock(int tickInterruptPin)
        {
            this.tickInterruptPin = tickInterruptPin;
            rtc = new Ds3231(I2cDevice.Create(new I2cConnectionSettings(1, Ds3231.DefaultI2cAddress)));
        }

        public void Start()
        {
            rtc.SetAlarm1(new Ds3231Alarm1(0, 0, 0, 0, Ds3231Alarm1MatchMode.OncePerSecond));
            rtc.SetEnabledAlarm(Ds3231Alarm.Alarm1);

            // Monitor the scheduling clock pin for interrupts from the RTC
            GpioController controller = new GpioController();
            controller.OpenPin(tickInterruptPin);
            controller.SetPinMode(tickInterruptPin, PinMode.Input);
            controller.RegisterCallbackForPinValueChangedEvent(tickInterruptPin, PinEventTypes.Falling, OnPinInterrupt);
        }

        private void OnPinInterrupt(object sender, PinValueChangedEventArgs pinValueChangedEventArgs)
        {
            rtc.ResetAlarmTriggeredStates();
            Ticked?.Invoke(sender, new ClockTickedEventArgs(rtc.DateTime));
        }
    }
}

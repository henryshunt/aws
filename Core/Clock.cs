using Iot.Device.Rtc;
using System;
using System.Device.Gpio;
using System.Device.I2c;

namespace AWS.Core
{
    internal class Clock
    {
        private int tickInterruptPin;
        private GpioController gpio;

        private Ds3231 rtc;

        public DateTime DateTime => rtc.DateTime;
        public bool IsClockDateTimeValid => rtc.IsDateTimeValid;

        public event EventHandler<ClockTickedEventArgs> Ticked;


        public Clock(int tickInterruptPin, GpioController gpio)
        {
            this.tickInterruptPin = tickInterruptPin;
            this.gpio = gpio;

            rtc = new Ds3231(I2cDevice.Create(new I2cConnectionSettings(1, Ds3231.DefaultI2cAddress)));
        }

        public void Start()
        {
            rtc.SetAlarm1(new Ds3231Alarm1(0, 0, 0, 0, Ds3231Alarm1MatchMode.OncePerSecond));
            rtc.SetEnabledAlarm(Ds3231Alarm.Alarm1);

            gpio.OpenPin(tickInterruptPin);
            gpio.SetPinMode(tickInterruptPin, PinMode.Input);
            gpio.RegisterCallbackForPinValueChangedEvent(tickInterruptPin, PinEventTypes.Falling, OnAlarmTriggered);
        }

        private void OnAlarmTriggered(object sender, PinValueChangedEventArgs pinValueChangedEventArgs)
        {
            rtc.ResetAlarmTriggeredStates();
            Ticked?.Invoke(sender, new ClockTickedEventArgs(rtc.DateTime));
        }
    }
}

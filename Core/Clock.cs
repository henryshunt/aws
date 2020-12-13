﻿using Iot.Device.Rtc;
using System;
using System.Device.Gpio;
using System.Device.I2c;

namespace AWS.Core
{
    internal class Clock
    {
        private readonly int sqwPin;
        private readonly GpioController gpio;

        private Ds3231 ds3231;

        public DateTime DateTime => ds3231.DateTime;
        public bool IsDateTimeValid => ds3231.IsDateTimeValid;

        public event EventHandler<ClockTickedEventArgs> Ticked;

        public Clock(int sqwPin, GpioController gpio)
        {
            this.sqwPin = sqwPin;
            this.gpio = gpio;
        }


        public void Open()
        {
            ds3231 = new Ds3231(
                I2cDevice.Create(new I2cConnectionSettings(1, Ds3231.DefaultI2cAddress)));

            ds3231.EnabledAlarm = Ds3231Alarm.None;
            ds3231.ResetAlarmTriggeredStates();
        }

        public void Start()
        {
            gpio.OpenPin(sqwPin);
            gpio.SetPinMode(sqwPin, PinMode.Input);
            gpio.RegisterCallbackForPinValueChangedEvent(sqwPin, PinEventTypes.Falling, OnSqwInterrupt);

            ds3231.SetAlarm1(new Ds3231Alarm1(0, 0, 0, 0, Ds3231Alarm1MatchMode.OncePerSecond));
            ds3231.EnabledAlarm = Ds3231Alarm.Alarm1;
        }

        private void OnSqwInterrupt(object sender, PinValueChangedEventArgs pinValueChangedEventArgs)
        {
            ds3231.ResetAlarmTriggeredStates();
            Ticked?.Invoke(sender, new ClockTickedEventArgs(ds3231.DateTime));
        }
    }
}

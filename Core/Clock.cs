using Iot.Device.Rtc;
using System;
using System.Device.Gpio;
using System.Device.I2c;

namespace Aws.Core
{
    /// <summary>
    /// Represents the system's real-time clock, which provides time data and control signals.
    /// </summary>
    internal class Clock : IDisposable
    {
        /// <summary>
        /// The pin that the DS3231's SQW pin is connected to.
        /// </summary>
        private readonly int sqwPin;

        private readonly GpioController gpio;
        private Ds3231 ds3231;

        /// <summary>
        /// The current time.
        /// </summary>
        public DateTime DateTime => ds3231.DateTime;

        /// <summary>
        /// Occurs at the start of every second once <see cref="StartTickEvents"/> has been called.
        /// </summary>
        public event EventHandler<ClockTickedEventArgs> Ticked;

        /// <summary>
        /// Initialises a new instance of the <see cref="Clock"/> class.
        /// </summary>
        /// <param name="sqwPin">
        /// The pin that the DS3231's SQW pin is connected to.
        /// </param>
        /// <param name="gpio">
        /// The GPIO controller.
        /// </param>
        public Clock(int sqwPin, GpioController gpio)
        {
            if (sqwPin < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sqwPin),
                    nameof(sqwPin) + " cannot not be less than zero");
            }

            this.sqwPin = sqwPin;
            this.gpio = gpio;
        }

        /// <summary>
        /// Opens the clock.
        /// </summary>
        public void Open()
        {
            ds3231 = new Ds3231(I2cDevice.Create(
                new I2cConnectionSettings(1, Ds3231.DefaultI2cAddress)));

            ds3231.EnabledAlarm = Ds3231Alarm.None;
            ds3231.ResetAlarmTriggeredStates();
        }

        /// <summary>
        /// Starts the triggering of the <see cref="Ticked"/> event at the start of every second.
        /// </summary>
        public void StartTickEvents()
        {
            gpio.OpenPin(sqwPin);
            gpio.SetPinMode(sqwPin, PinMode.Input);
            gpio.RegisterCallbackForPinValueChangedEvent(sqwPin, PinEventTypes.Falling,
                OnSqwInterrupt);

            Ds3231AlarmOne alarm = new Ds3231AlarmOne(0, new TimeSpan(0, 0, 0, 0, 0),
                Ds3231AlarmOneMatchMode.OncePerSecond);

            ds3231.SetAlarmOne(alarm);
            ds3231.EnabledAlarm = Ds3231Alarm.AlarmOne;
        }

        /// <summary>
        /// Stops the triggering of the <see cref="Ticked"/> event at the start of every second.
        /// </summary>
        public void StopTickEvents()
        {
            gpio.UnregisterCallbackForPinValueChangedEvent(sqwPin, OnSqwInterrupt);

            ds3231.EnabledAlarm = Ds3231Alarm.None;
            ds3231.ResetAlarmTriggeredStates();
        }

        private void OnSqwInterrupt(object sender, PinValueChangedEventArgs e)
        {
            ds3231.ResetAlarmTriggeredStates();
            Ticked?.Invoke(sender, new ClockTickedEventArgs(ds3231.DateTime));
        }

        /// <summary>
        /// Disposes the clock.
        /// </summary>
        public void Dispose()
        {
            StopTickEvents();
            ds3231.Dispose();
        }
    }
}

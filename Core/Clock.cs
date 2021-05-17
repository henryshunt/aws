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

        /// <summary>
        /// Indicates whether the clock is open.
        /// </summary>
        public bool IsOpen { get; private set; } = false;

        /// <summary>
        /// Indicates whether triggering of the <see cref="Ticked"/> event at the start of every second is enabled.
        /// </summary>
        public bool IsTickEventsEnabled { get; private set; } = false;

        private Ds3231 ds3231;

        /// <summary>
        /// The current time.
        /// </summary>
        public DateTime DateTime => ds3231.DateTime;

        /// <summary>
        /// Occurs at the start of every second once <see cref="EnableTickEvents"/> has been called.
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
        /// <exception cref="InvalidOperationException">
        /// Thrown if the clock is already open.
        /// </exception>
        public void Open()
        {
            if (IsOpen)
                throw new InvalidOperationException("The clock is already open");
            IsOpen = true;

            ds3231 = new Ds3231(I2cDevice.Create(
                new I2cConnectionSettings(1, Ds3231.DefaultI2cAddress)))
            {
                EnabledAlarm = Ds3231Alarm.None
            };

            ds3231.ResetAlarmTriggeredStates();
        }

        /// <summary>
        /// Closes the clock.
        /// </summary>
        public void Close()
        {
            if (IsTickEventsEnabled)
                DisableTickEvents();

            ds3231?.Dispose();
            IsOpen = false;
        }

        /// <summary>
        /// Enables triggering of the <see cref="Ticked"/> event at the start of every second.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the clock is not open or tick events are already enabled.
        /// </exception>
        public void EnableTickEvents()
        {
            if (!IsOpen)
                throw new InvalidOperationException("The clock is not open");

            if (IsTickEventsEnabled)
                throw new InvalidOperationException("Tick events are already enabled");
            IsTickEventsEnabled = true;

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
        /// <exception cref="InvalidOperationException">
        /// Thrown if the clock is not open.
        /// </exception>
        public void DisableTickEvents()
        {
            if (!IsOpen)
                throw new InvalidOperationException("The clock is not open");

            gpio.UnregisterCallbackForPinValueChangedEvent(sqwPin, OnSqwInterrupt);
            ds3231.EnabledAlarm = Ds3231Alarm.None;
            ds3231.ResetAlarmTriggeredStates();
            IsTickEventsEnabled = false;
        }

        private void OnSqwInterrupt(object sender, PinValueChangedEventArgs e)
        {
            ds3231.ResetAlarmTriggeredStates();
            Ticked?.Invoke(sender, new ClockTickedEventArgs(ds3231.DateTime));
        }

        public void Dispose() => Close();
    }
}

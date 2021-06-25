using Iot.Device.Rtc;
using System;
using System.Device.Gpio;
using System.Device.I2c;

namespace Aws.Core
{
    /// <summary>
    /// Represents the system's clock, which provides time data and control signals.
    /// </summary>
    internal class Clock : IDisposable
    {
        /// <summary>
        /// The pin that the DS3231's INT/SQW pin is connected to.
        /// </summary>
        private readonly int intSqwPin;

        private readonly GpioController gpio;

        /// <summary>
        /// Indicates whether the clock is open.
        /// </summary>
        public bool IsOpen { get; private set; } = false;

        private Ds3231 ds3231;

        /// <summary>
        /// The current time.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the clock is not open.
        /// </exception>
        public DateTime Time
        {
            get
            {
                if (!IsOpen)
                    throw new InvalidOperationException("The clock is not open");

                return ds3231.DateTime;
            }
        }

        /// <summary>
        /// Indicates whether the time is valid.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the clock is not open.
        /// </exception>
        public bool IsTimeValid
        {
            get
            {
                if (!IsOpen)
                    throw new InvalidOperationException("The clock is not open");

                return ds3231.IsDateTimeValid;
            }
        }

        /// <summary>
        /// Occurs at the start of every second.
        /// </summary>
        public event EventHandler<ClockTickedEventArgs> Ticked;

        /// <summary>
        /// Initialises a new instance of the <see cref="Clock"/> class.
        /// </summary>
        /// <param name="intSqwPin">
        /// The pin that the DS3231's INT/SQW pin is connected to.
        /// </param>
        /// <param name="gpio">
        /// The GPIO controller.
        /// </param>
        public Clock(int intSqwPin, GpioController gpio)
        {
            if (intSqwPin < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(intSqwPin),
                    nameof(intSqwPin) + " cannot not be less than zero");
            }

            this.intSqwPin = intSqwPin;
            this.gpio = gpio;
        }

        /// <summary>
        /// Opens the clock and begins invoking the <see cref="Ticked"/> event every second.
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
                IntSqwPinMode = Ds3231IntSqwPinMode.SquareWave,
                SquareWaveRate = Ds3231SquareWaveRate.Rate1Hz
            };

            gpio.OpenPin(intSqwPin);
            gpio.SetPinMode(intSqwPin, PinMode.Input);
            gpio.RegisterCallbackForPinValueChangedEvent(intSqwPin, PinEventTypes.Falling,
                OnSqwInterrupt);
        }

        /// <summary>
        /// Closes the clock.
        /// </summary>
        public void Close()
        {
            if (IsOpen)
            {
                gpio.UnregisterCallbackForPinValueChangedEvent(
                    intSqwPin, OnSqwInterrupt);
            }

            ds3231?.Dispose();
            IsOpen = false;
        }

        private void OnSqwInterrupt(object sender, PinValueChangedEventArgs e)
        {
            Ticked?.Invoke(sender, new ClockTickedEventArgs(ds3231.DateTime));
        }

        public void Dispose() => Close();
    }
}

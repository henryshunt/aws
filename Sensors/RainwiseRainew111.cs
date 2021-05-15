using System.Device.Gpio;
using System.Threading;

namespace Aws.Sensors
{
    /// <summary>
    /// Represents the Rainwise Rainew 111 sensor.
    /// </summary>
    internal class RainwiseRainew111 : ISensor
    {
        private readonly GpioController gpio;

        /// <summary>
        /// The pin that the sensor is connected to.
        /// </summary>
        private readonly int pin;

        /// <summary>
        /// Stores the number of bucket tips since the last sample.
        /// </summary>
        private volatile int counter = 0;

        /// <summary>
        /// The amount of rainfall that one bucket tip equates to, in millimetres.
        /// </summary>
        private const double MM_PER_BUCKET_TIP = 0.254;

        /// <summary>
        /// Initialises a new instance of the <see cref="RainwiseRainew111"/> sensor.
        /// </summary>
        /// <param name="pin">The pin that the sensor is connected to.</param>
        /// <param name="gpio">The GPIO controller.</param>
        public RainwiseRainew111(int pin, GpioController gpio)
        {
            this.pin = pin;
            this.gpio = gpio;
        }

        /// <summary>
        /// Opens the sensor.
        /// </summary>
        public void Open()
        {
            gpio.OpenPin(pin, PinMode.InputPullUp);
            gpio.RegisterCallbackForPinValueChangedEvent(pin, PinEventTypes.Falling,
                OnBucketTip);
        }

        /// <summary>
        /// Samples the sensor.
        /// </summary>
        /// <returns>The amount of rainfall since the last sample, in millimetres.</returns>
        public double Sample()
        {
            return Interlocked.Exchange(ref counter, 0) * MM_PER_BUCKET_TIP;
        }

        private void OnBucketTip(object sender, PinValueChangedEventArgs e)
        {
            counter++;
        }

        public void Dispose()
        {
            gpio.UnregisterCallbackForPinValueChangedEvent(pin, OnBucketTip);
        }
    }
}

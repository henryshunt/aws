using System;
using System.Device.Gpio;

namespace Aws.Sensors
{
    internal class RainwiseRainew111
    {
        private GpioController gpio;
        private int pin;

        private bool isStarted = false;
        private int counter = 0;

        public RainwiseRainew111(GpioController gpio, int pin)
        {
            this.gpio = gpio;
            this.pin = pin;
        }

        public void Open()
        {
            gpio.OpenPin(pin, PinMode.InputPullUp);
        }

        public void Start()
        {
            if (!isStarted)
            {
                gpio.RegisterCallbackForPinValueChangedEvent(pin, PinEventTypes.Falling, OnBucketTip);
                isStarted = true;
            }
        }

        public double Sample()
        {
            if (!isStarted)
                return 0;

            int counter = this.counter;
            this.counter = 0;

            return counter * 0.254;
        }

        private void OnBucketTip(object sender, PinValueChangedEventArgs pinValueChangedEventArgs)
        {
            counter++;
        }
    }
}

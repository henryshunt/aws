using System.Device.Gpio;

namespace AWS.Hardware
{
    internal class RainwiseRainew111
    {
        public static double RainfallMMPerCount = 0.254;

        public bool IsPaused = true;
        private int counter = 0;

        public void Initialise(int pinNumber)
        {
            GpioController gpio = new GpioController(PinNumberingScheme.Logical);
            gpio.OpenPin(pinNumber, PinMode.InputPullUp);
            gpio.RegisterCallbackForPinValueChangedEvent(pinNumber, PinEventTypes.Falling, OnInterrupt);
        }

        public int Sample()
        {
            int counter = this.counter;
            this.counter = 0;
            
            return counter;
        }

        private void OnInterrupt(object sender, PinValueChangedEventArgs pinValueChangedEventArgs)
        {
            if (IsPaused)
                return;

            counter++;
        }
    }
}

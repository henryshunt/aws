using System.Device.Gpio;

namespace AWS.Hardware
{
    internal class RainwiseRainew111
    {
        public static double MMPerBucketTip = 0.254;

        public bool ManualInputMode { get; private set; }
        public bool IsPaused = true;

        //public CounterValueStore RainfallStore { get; } = new CounterValueStore();


        public void Initialise()
        {
            ManualInputMode = true;
        }
        public void Initialise(int pinNumber)
        {
            ManualInputMode = false;

            GpioController gpio = new GpioController(PinNumberingScheme.Logical);
            gpio.OpenPin(pinNumber, PinMode.InputPullUp);
            gpio.RegisterCallbackForPinValueChangedEvent(pinNumber, PinEventTypes.Falling, OnInterrupt);
        }

        private void OnInterrupt(object sender, PinValueChangedEventArgs pinValueChangedEventArgs)
        {
            if (IsPaused) return;
            //RainfallStore.ActiveValueBucket++;
        }
    }
}

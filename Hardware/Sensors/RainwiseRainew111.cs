using System;
using System.Device.Gpio;
using static AWS.Routines.Helpers;

namespace AWS.Hardware.Sensors
{
    internal class RainwiseRainew111
    {
        public bool IsPaused { get; set; } = true;
        public SamplingBucket SamplingBucket { get; private set; } = SamplingBucket.Bucket1;

        private int SamplingBucket1 = 0;
        private int SamplingBucket2 = 0;

        public bool Initialise(int pinNumber)
        {
            GpioController gpio = new GpioController(PinNumberingScheme.Logical);
            gpio.OpenPin(pinNumber, PinMode.InputPullUp);
            gpio.RegisterCallbackForPinValueChangedEvent(pinNumber, PinEventTypes.Rising, OnInterrupt);
            return true;
        }


        private void OnInterrupt(object sender, PinValueChangedEventArgs pinValueChangedEventArgs)
        {
            if (IsPaused) return;
            if (SamplingBucket == SamplingBucket.Bucket1)
                SamplingBucket1++;
            else SamplingBucket2++;
        }

        public double CalculateTotal(SamplingBucket samplingBucket)
        {
            if (samplingBucket == SamplingBucket.Bucket1)
                return SamplingBucket1 * 0.254;
            else return SamplingBucket2 * 0.254;
        }

        public void SwitchSamplingBucket()
        {
            if (SamplingBucket == SamplingBucket.Bucket1)
                SamplingBucket = SamplingBucket.Bucket2;
            else SamplingBucket = SamplingBucket.Bucket1;
        }
        public void EmptySamplingBucket(SamplingBucket samplingBucket)
        {
            if (samplingBucket == SamplingBucket.Bucket1)
                SamplingBucket1 = 0;
            else SamplingBucket2 = 0;
        }
    }
}

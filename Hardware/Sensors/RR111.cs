using System;
using System.Device.Gpio;
using static AWS.Routines.Helpers;

namespace AWS.Hardware.Sensors
{
    internal class RR111
    {
        public bool IsPaused { get; set; } = true;
        public SamplingBucket SamplingBucket { get; private set; } = SamplingBucket.Bucket1;

        private int SamplingBucket1 = 0;
        private int SamplingBucket2 = 0;

        public bool Setup(int pinNumber)
        {
            var pins = new GpioController(PinNumberingScheme.Logical);
            pins.OpenPin(pinNumber, PinMode.InputPullUp);
            pins.RegisterCallbackForPinValueChangedEvent(pinNumber, PinEventTypes.Rising, OnInterrupt);
            return true;
        }

        private void OnTransferReady(object sender, PinValueChangedEventArgs pinValueChangedEventArgs)
        {
            throw new NotImplementedException();
        }

        private void OnInterrupt(object sender, PinValueChangedEventArgs pinValueChangedEventArgs)
        {
            if (IsPaused) return;
            if (SamplingBucket == SamplingBucket.Bucket1)
                SamplingBucket1++;
            else SamplingBucket2++;
        }

        public void SwitchSamplingBucket()
        {
            if (SamplingBucket == SamplingBucket.Bucket1)
                SamplingBucket = SamplingBucket.Bucket2;
            else SamplingBucket = SamplingBucket.Bucket1;
        }

        public double CalculateTotal(SamplingBucket samplingBucket)
        {
            if (samplingBucket == SamplingBucket.Bucket1)
                return SamplingBucket1 * 0.254;
            else return SamplingBucket2 * 0.254;
        }

        public void EmptySamplingBucket(SamplingBucket samplingBucket)
        {
            if (samplingBucket == SamplingBucket.Bucket1)
                SamplingBucket1 = 0;
            else SamplingBucket2 = 0;
        }
    }
}

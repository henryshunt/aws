namespace AWS.Core
{
    internal class SampleStoreAlternator
    {
        public SampleStore ActiveSampleStore
        {
            get
            {
                if (sampleStoreInUse == 1)
                    return sampleStore1;
                else return sampleStore2;
            }
        }

        public SampleStore InactiveSampleStore
        {
            get
            {
                if (sampleStoreInUse == 1)
                    return sampleStore2;
                else return sampleStore1;
            }
        }

        private int sampleStoreInUse = 1;

        private readonly SampleStore sampleStore1 = new SampleStore();
        private readonly SampleStore sampleStore2 = new SampleStore();

        public void SwapSampleStore()
        {
            if (sampleStoreInUse == 1)
                sampleStoreInUse = 2;
            else sampleStoreInUse = 1;
        }
    }
}

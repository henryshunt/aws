namespace AWS.Core
{
    internal class SampleStoreAlternator
    {
        public SampleStore ActiveSampleStore
        {
            get
            {
                if (sampleStoreInUse == SampleStoreOption.Bucket1)
                    return sampleStore1;
                else return sampleStore2;
            }
        }

        public SampleStore InactiveSampleStore
        {
            get
            {
                if (sampleStoreInUse == SampleStoreOption.Bucket1)
                    return sampleStore2;
                else return sampleStore1;
            }
        }

        private SampleStoreOption sampleStoreInUse = SampleStoreOption.Bucket1;

        private readonly SampleStore sampleStore1 = new SampleStore();
        private readonly SampleStore sampleStore2 = new SampleStore();

        public void SwapSampleStore()
        {
            if (sampleStoreInUse == SampleStoreOption.Bucket1)
                sampleStoreInUse = SampleStoreOption.Bucket2;
            else sampleStoreInUse = SampleStoreOption.Bucket1;
        }

        public enum SampleStoreOption { Bucket1, Bucket2 }
    }
}

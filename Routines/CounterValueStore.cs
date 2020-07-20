using static AWS.Routines.Helpers;

namespace AWS.Routines
{
    internal class CounterValueStore
    {
        public int ActiveValueBucket
        {
            get
            {
                if (ValueBucketInUse == ValueBucket.Bucket1)
                    return ValueBucket1;
                else return ValueBucket2;
            }
            set
            {
                if (ValueBucketInUse == ValueBucket.Bucket1)
                    ValueBucket1 = value;
                else ValueBucket2 = value;
            }
        }
        public int InactiveValueBucket
        {
            get
            {
                if (ValueBucketInUse == ValueBucket.Bucket1)
                    return ValueBucket2;
                else return ValueBucket1;
            }
            set
            {
                if (ValueBucketInUse == ValueBucket.Bucket1)
                    ValueBucket2 = value;
                else ValueBucket1 = value;
            }
        }

        private ValueBucket ValueBucketInUse = ValueBucket.Bucket1;

        private int ValueBucket1 = 0;
        private int ValueBucket2 = 0;

        public void SwapValueBucket()
        {
            if (ValueBucketInUse == ValueBucket.Bucket1)
                ValueBucketInUse = ValueBucket.Bucket2;
            else ValueBucketInUse = ValueBucket.Bucket1;
        }
    }
}

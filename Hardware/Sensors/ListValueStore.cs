using System.Collections.Generic;
using static AWS.Routines.Helpers;

namespace AWS.Hardware.Sensors
{
    internal class ListValueStore<T>
    {
        public List<T> ActiveValueBucket
        {
            get
            {
                if (ValueBucketInUse == ValueBucket.Bucket1)
                    return ValueBucket1;
                else return ValueBucket2;
            }
        }
        public List<T> InactiveValueBucket
        {
            get
            {
                if (ValueBucketInUse == ValueBucket.Bucket1)
                    return ValueBucket2;
                else return ValueBucket1;
            }
        }

        private ValueBucket ValueBucketInUse = ValueBucket.Bucket1;

        private readonly List<T> ValueBucket1 = new List<T>();
        private readonly List<T> ValueBucket2 = new List<T>();

        public void SwapValueBucket()
        {
            if (ValueBucketInUse == ValueBucket.Bucket1)
                ValueBucketInUse = ValueBucket.Bucket2;
            else ValueBucketInUse = ValueBucket.Bucket1;
        }
    }
}

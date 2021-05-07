using Aws.Misc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Aws.Core
{
    /// <summary>
    /// Caches wind speed and direction samples and allows for calculating the final summary values.
    /// </summary>
    internal class WindMonitor
    {
        /// <summary>
        /// Caches wind speed samples.
        /// </summary>
        private readonly List<KeyValuePair<DateTime, double>> speedCache
            = new List<KeyValuePair<DateTime, double>>();

        /// <summary>
        /// Caches wind direction samples.
        /// </summary>
        private readonly List<KeyValuePair<DateTime, double>> directionCache
            = new List<KeyValuePair<DateTime, double>>();

        /// <summary>
        /// Stores the time that new samples were last added to the cache.
        /// </summary>
        private DateTime lastCacheTime;

        /// <summary>
        /// Initialises a new instance of the <see cref="WindMonitor"/> class.
        /// </summary>
        public WindMonitor() { }

        /// <summary>
        /// Caches new samples and removes any samples that are ten minutes old or older from the cache.
        /// </summary>
        /// <param name="time">
        /// The current time.
        /// </param>
        /// <param name="speedSamples">
        /// The wind speed samples to cache.
        /// </param>
        /// <param name="directionSamples">
        /// The wind direction samples to cache.
        /// </param>
        public void CacheSamples(DateTime time, List<KeyValuePair<DateTime, double>> speedSamples,
            List<KeyValuePair<DateTime, double>> directionSamples)
        {
            lastCacheTime = time;

            speedCache.AddRange(speedSamples);
            directionCache.AddRange(directionSamples);

            DateTime tenMinuteStart = time - TimeSpan.FromMinutes(10);
            speedCache.RemoveAll(sample => sample.Key <= tenMinuteStart);
            directionCache.RemoveAll(sample => sample.Key <= tenMinuteStart);
        }

        /// <summary>
        /// Calculates, for the ten minutes leading up to the last time that new samples were cached, the average wind
        /// speed and direction, and maximum three-second gust, of the cached samples.
        /// </summary>
        /// <param name="time">
        /// The current time.
        /// </param>
        /// <returns>
        /// A tuple containing the calculated wind speed, direction and gust.
        /// </returns>
        public (double?, double?, double?) CalculateSummaryValues()
        {
            double? windSpeed = null;
            if (speedCache.Count > 0)
                windSpeed = speedCache.Average(x => x.Value);

            double? windDirection = null;
            if (windSpeed != null && windSpeed > 0 && directionCache.Count > 0)
            {
                List<Vector> vectors = new List<Vector>();

                // Create a vector (speed and direction pair) for each second in the 10-minute period
                for (DateTime i = lastCacheTime - TimeSpan.FromSeconds(599);
                    i <= lastCacheTime; i += TimeSpan.FromSeconds(1))
                {
                    if (speedCache.Any(s => s.Key == i) && directionCache.Any(s => s.Key == i))
                    {
                        double magnitude = speedCache.Single(s => s.Key == i).Value;
                        double direction = directionCache.Single(s => s.Key == i).Value;
                        vectors.Add(new Vector(magnitude, direction));
                    }
                }

                if (vectors.Count > 0)
                    windDirection = Utilities.VectorDirectionAverage(vectors);
            }

            double? windGust = null;
            if (speedCache.Count > 0)
            {
                windGust = 0;

                // Find the highest 3-second average wind speed in the stored data. A 3-second
                // average includes the samples <= second T and > T-3
                for (DateTime i = lastCacheTime - TimeSpan.FromMinutes(10);
                    i <= lastCacheTime - TimeSpan.FromSeconds(3); i += TimeSpan.FromSeconds(1))
                {
                    var gustSamples = speedCache.Where(
                        s => s.Key > i && s.Key <= i + TimeSpan.FromSeconds(3));

                    if (gustSamples.Any())
                    {
                        double gustSample = gustSamples.Average(s => s.Value);

                        if (gustSample > windGust)
                            windGust = gustSample;
                    }
                }
            }

            return (windSpeed, windDirection, windGust);
        }
    }
}

using Aws.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using static Aws.Misc.Utilities;

namespace Aws.Core
{
    /// <summary>
    /// Buffers wind speed and direction samples and allows for calculating the final summary values.
    /// </summary>
    internal class WindMonitor
    {
        /// <summary>
        /// Buffers wind speed samples.
        /// </summary>
        private readonly List<KeyValuePair<DateTime, double>> speedBuffer
            = new List<KeyValuePair<DateTime, double>>();

        /// <summary>
        /// Buffers wind direction samples.
        /// </summary>
        private readonly List<KeyValuePair<DateTime, double>> directionBuffer
            = new List<KeyValuePair<DateTime, double>>();

        /// <summary>
        /// Stores the time that new samples were last added to the buffers, in UTC.
        /// </summary>
        private DateTime lastBufferTime;

        /// <summary>
        /// Initialises a new instance of the <see cref="WindMonitor"/> class.
        /// </summary>
        public WindMonitor() { }

        /// <summary>
        /// Buffers new samples and removes any samples that are ten minutes old or older from the buffers.
        /// </summary>
        /// <param name="time">
        /// The current time, in UTC.
        /// </param>
        /// <param name="speedSamples">
        /// The wind speed samples to buffer. Times should be in UTC.
        /// </param>
        /// <param name="directionSamples">
        /// The wind direction samples to buffer. Times should be in UTC.
        /// </param>
        public void BufferSamples(DateTime time, List<KeyValuePair<DateTime, double>> speedSamples,
            List<KeyValuePair<DateTime, double>> directionSamples)
        {
            lastBufferTime = time;

            speedBuffer.AddRange(speedSamples);
            directionBuffer.AddRange(directionSamples);

            DateTime tenMinuteStart = time - TimeSpan.FromMinutes(10);
            speedBuffer.RemoveAll(sample => sample.Key <= tenMinuteStart);
            directionBuffer.RemoveAll(sample => sample.Key <= tenMinuteStart);
        }

        /// <summary>
        /// Calculates, for the ten minutes leading up to the last time that new samples were buffered, the average
        /// wind speed and direction, and maximum three-second gust, of the buffered samples.
        /// </summary>
        /// <param name="time">
        /// The current time, in UTC.
        /// </param>
        /// <returns>
        /// A tuple containing the calculated wind speed, direction and gust.
        /// </returns>
        public (double?, double?, double?) CalculateSummaryValues()
        {
            double? windSpeed = null;
            if (speedBuffer.Count > 0)
                windSpeed = speedBuffer.Average(x => x.Value);

            double? windDirection = null;
            if (windSpeed != null && windSpeed > 0 && directionBuffer.Count > 0)
            {
                List<Vector> vectors = new List<Vector>();

                // Create a vector (speed and direction pair) for each second in the 10-minute period
                for (DateTime i = lastBufferTime - TimeSpan.FromSeconds(599);
                    i <= lastBufferTime; i += TimeSpan.FromSeconds(1))
                {
                    if (speedBuffer.Any(s => s.Key == i) && directionBuffer.Any(s => s.Key == i))
                    {
                        double magnitude = speedBuffer.Single(s => s.Key == i).Value;
                        double direction = directionBuffer.Single(s => s.Key == i).Value;
                        vectors.Add(new Vector(magnitude, direction));
                    }
                }

                if (vectors.Count > 0)
                    windDirection = VectorDirectionAverage(vectors);
            }

            double? windGust = null;
            if (speedBuffer.Count > 0)
            {
                windGust = 0;

                // Find the highest 3-second average wind speed in the stored data. A 3-second
                // average includes the samples <= second T and > T-3
                for (DateTime i = lastBufferTime - TimeSpan.FromMinutes(10);
                    i <= lastBufferTime - TimeSpan.FromSeconds(3); i += TimeSpan.FromSeconds(1))
                {
                    var gustSamples = speedBuffer.Where(
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

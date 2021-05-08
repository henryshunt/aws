namespace Aws.Misc
{
    /// <summary>
    /// Represents a measurement with a magnitude and direction.
    /// </summary>
    internal class Vector
    {
        /// <summary>
        /// The magnitude of the measurement.
        /// </summary>
        public double Magnitude { get; set; }

        /// <summary>
        /// The direction of the measurement.
        /// </summary>
        public double Direction { get; set; }

        /// <summary>
        /// Initialises a new instance of the <see cref="Vector"/> class.
        /// </summary>
        /// <param name="magnitude">
        /// The magnitude of the measurement.
        /// </param>
        /// <param name="direction">
        /// The direction of the measurement.
        /// </param>
        public Vector(double magnitude, double direction)
        {
            Magnitude = magnitude;
            Direction = direction;
        }
    }
}

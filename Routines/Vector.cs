namespace Aws.Routines
{
    public class Vector
    {
        public double Magnitude { get; set; }
        public double Direction { get; set; }

        public Vector(double magnitude, double direction)
        {
            Magnitude = magnitude;
            Direction = direction;
        }
    }
}

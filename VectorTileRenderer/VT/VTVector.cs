using System;

namespace VectorTileRenderer
{
    // based on Xamarin Forms
    public struct VTVector
    {
        public VTVector(double x, double y)
            : this()
        {
            X = x;
            Y = y;
        }


        public VTVector(VTPoint p)
            : this()
        {
            X = p.X;
            Y = p.Y;
        }

        public VTVector(double angle)
            : this()
        {
            X = Math.Cos(Math.PI * angle / 180);
            Y = Math.Sin(Math.PI * angle / 180);
        }

        public double X { private set; get; }
        public double Y { private set; get; }

        public double LengthSquared
        {
            get { return X * X + Y * Y; }
        }

        public double Length
        {
            get { return Math.Sqrt(LengthSquared); }
        }

        public VTVector Normalized
        {
            get
            {
                double length = Length;

                if (length != 0)
                {
                    return new VTVector(X / length, Y / length);
                }
                return new VTVector();
            }
        }

        public static double AngleBetween(VTVector v1, VTVector v2)
        {
            return 180 * (Math.Atan2(v2.Y, v2.X) - Math.Atan2(v1.Y, v1.X)) / Math.PI;
        }

        public static explicit operator VTPoint(VTVector v)
        {
            return new VTPoint(v.X, v.Y);
        }

    }

}

using System;
using System.Globalization;

namespace VectorTileRenderer
{
    public struct VTPoint
    {
        public VTPoint(double x, double y)
        {
            X = x;
            Y = y;
        }


        public double X { get; set; }
        public double Y { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is VTPoint p)
            {
                return this == p;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return X.GetHashCode() ^ (Y.GetHashCode() * 397);
        }

        public override string ToString()
        {
            return string.Format("{{X={0} Y={1}}}", X.ToString(CultureInfo.InvariantCulture), Y.ToString(CultureInfo.InvariantCulture));
        }

        public static bool operator ==(VTPoint p1, VTPoint p2) => (p1.X == p2.X) && (p1.Y == p2.Y);

        public static bool operator !=(VTPoint p1, VTPoint p2) => (p1.X != p2.X) || (p1.Y != p2.Y);

        public double Distance(VTPoint other)
        {
            return Math.Sqrt(Math.Pow(X - other.X, 2) + Math.Pow(Y - other.Y, 2));
        }

        public VTPoint Offset(double dx, double dy)
        {
            VTPoint p = this;
            p.X += dx;
            p.Y += dy;
            return p;
        }

        public VTPoint Round()
        {
            return new VTPoint(Math.Round(X), Math.Round(Y));
        }

        public bool IsEmpty
        {
            get { return (X == 0) && (Y == 0); }
        }

        public static explicit operator VTSize(VTPoint pt)
        {
            return new VTSize(pt.X, pt.Y);
        }
    }

}

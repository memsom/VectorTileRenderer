using System;
using System.Drawing;

namespace VectorTileRenderer
{
    public struct VTRect
    {
        public VTRect(double x, double y, double width, double height) : this()
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public VTRect(VTPoint tl, VTPoint br) : this()
        {
            X = tl.X;
            Y = tl.Y;
            Width = br.X - tl.X;
            Height = br.Y - tl.Y;
        }

        public VTRect(VTPoint loc, Size sz) : this(loc.X, loc.Y, sz.Width, sz.Height)
        {
        }

        public double X { get; set; }

        public double Y { get; set; }

        public double Width { get; set; }

        public double Height { get; set; }

        public static VTRect Zero = new VTRect();

        public double Top => Y;

        public double Bottom => Y + Height;

        public double Right => X + Width;

        public double Left => X;

        public bool IsEmpty => (Width <= 0) || (Height <= 0);

        public VTPoint Center => new VTPoint(X + Width / 2, Y + Height / 2);

        public VTSize Size
        {
            get => new VTSize(Width, Height);
            set
            {
                Width = value.Width;
                Height = value.Height;
            }
        }

        public VTPoint Location
        {
            get => new VTPoint(X, Y);
            set
            {
                X = value.X;
                Y = value.Y;
            }
        }

        public static VTRect FromLTRB(double left, double top, double right, double bottom) => new VTRect(left, top, right - left, bottom - top);

        public bool Equals(VTRect other) => X.Equals(other.X) && Y.Equals(other.Y) && Width.Equals(other.Width) && Height.Equals(other.Height);

        public override bool Equals(object obj)
        {
            if (obj is null)
                return false;

            return obj is VTRect rect && Equals(rect) || obj is Rectangle rectangle && Equals(rectangle);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = X.GetHashCode();
                hashCode = (hashCode * 397) ^ Y.GetHashCode();
                hashCode = (hashCode * 397) ^ Width.GetHashCode();
                hashCode = (hashCode * 397) ^ Height.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(VTRect r1, VTRect r2) => (r1.Location == r2.Location) && (r1.Size == r2.Size);

        public static bool operator !=(VTRect r1, VTRect r2) => !(r1 == r2);

        // Hit Testing / Intersection / Union
        public bool Contains(VTRect rect) => X <= rect.X && Right >= rect.Right && Y <= rect.Y && Bottom >= rect.Bottom;

        public bool Contains(VTPoint pt) => Contains(pt.X, pt.Y);

        public bool Contains(double x, double y) => (x >= Left) && (x < Right) && (y >= Top) && (y < Bottom);

        public bool IntersectsWith(VTRect r) => !((Left >= r.Right) || (Right <= r.Left) || (Top >= r.Bottom) || (Bottom <= r.Top));

        public VTRect Union(VTRect r) => Union(this, r);

        public static VTRect Union(VTRect r1, VTRect r2) => FromLTRB(Math.Min(r1.Left, r2.Left), Math.Min(r1.Top, r2.Top), Math.Max(r1.Right, r2.Right), Math.Max(r1.Bottom, r2.Bottom));

        public VTRect Intersect(VTRect r) => Intersect(this, r);

        public static VTRect Intersect(VTRect r1, VTRect r2)
        {
            double x = Math.Max(r1.X, r2.X);
            double y = Math.Max(r1.Y, r2.Y);
            double width = Math.Min(r1.Right, r2.Right) - x;
            double height = Math.Min(r1.Bottom, r2.Bottom) - y;

            if (width < 0 || height < 0)
                return Zero;

            return new VTRect(x, y, width, height);
        }

        // Inflate and Offset
        public VTRect Inflate(VTSize sz) => Inflate(sz.Width, sz.Height);

        public VTRect Inflate(double width, double height)
        {
            VTRect r = this;
            r.X -= width;
            r.Y -= height;
            r.Width += width * 2;
            r.Height += height * 2;
            return r;
        }

        public VTRect Offset(double dx, double dy)
        {
            VTRect r = this;
            r.X += dx;
            r.Y += dy;
            return r;
        }

        public VTRect Offset(VTPoint dr) => Offset(dr.X, dr.Y);

        public VTRect Round() => new VTRect(Math.Round(X), Math.Round(Y), Math.Round(Width), Math.Round(Height));

        public void Deconstruct(out double x, out double y, out double width, out double height)
        {
            x = X;
            y = Y;
            width = Width;
            height = Height;
        }
    }

}

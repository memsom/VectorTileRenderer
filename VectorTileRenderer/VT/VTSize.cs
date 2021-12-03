using System;
using System.Globalization;

namespace VectorTileRenderer
{
    public struct VTSize
    {
        double _width;
        double _height;

        public static readonly VTSize Zero;

        public VTSize(double width, double height)
        {
            if (double.IsNaN(width))
                throw new ArgumentException("NaN is not a valid value for width");
            if (double.IsNaN(height))
                throw new ArgumentException("NaN is not a valid value for height");
            _width = width;
            _height = height;
        }

        public bool IsZero
        {
            get { return (_width == 0) && (_height == 0); }
        }

        public double Width
        {
            get { return _width; }
            set
            {
                if (double.IsNaN(value))
                    throw new ArgumentException("NaN is not a valid value for Width");
                _width = value;
            }
        }

        public double Height
        {
            get { return _height; }
            set
            {
                if (double.IsNaN(value))
                    throw new ArgumentException("NaN is not a valid value for Height");
                _height = value;
            }
        }

        public static VTSize operator +(VTSize s1, VTSize s2)
        {
            return new VTSize(s1._width + s2._width, s1._height + s2._height);
        }

        public static VTSize operator -(VTSize s1, VTSize s2)
        {
            return new VTSize(s1._width - s2._width, s1._height - s2._height);
        }

        public static VTSize operator *(VTSize s1, double value)
        {
            return new VTSize(s1._width * value, s1._height * value);
        }

        public static bool operator ==(VTSize s1, VTSize s2)
        {
            return (s1._width == s2._width) && (s1._height == s2._height);
        }

        public static bool operator !=(VTSize s1, VTSize s2)
        {
            return (s1._width != s2._width) || (s1._height != s2._height);
        }

        public static explicit operator VTPoint(VTSize size)
        {
            return new VTPoint(size.Width, size.Height);
        }

        public bool Equals(VTSize other)
        {
            return _width.Equals(other._width) && _height.Equals(other._height);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is VTSize && Equals((VTSize)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (_width.GetHashCode() * 397) ^ _height.GetHashCode();
            }
        }

        public override string ToString()
        {
            return string.Format("{{Width={0} Height={1}}}", _width.ToString(CultureInfo.InvariantCulture), _height.ToString(CultureInfo.InvariantCulture));
        }

        public void Deconstruct(out double width, out double height)
        {
            width = Width;
            height = Height;
        }
    }

}

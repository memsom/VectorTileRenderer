using System;

namespace VectorTileRenderer
{
    class ComparableColor : IComparable
    {
        private long numericColor;

        public ComparableColor(string encodedColor)
        {

        }

        public int CompareTo(object obj)
        {
            if (obj.GetType() != typeof(ComparableColor))
            {
                return -1;
            }

            return numericColor.CompareTo((ComparableColor)obj);
        }
    }
}

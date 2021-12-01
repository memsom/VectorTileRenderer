using System;

namespace ClipperLib
{
	public struct IntPoint
	{
		public long X;

		public long Y;

		public IntPoint(long X, long Y)
		{
			this.X = X;
			this.Y = Y;
		}

		public IntPoint(IntPoint pt)
		{
			this.X = pt.X;
			this.Y = pt.Y;
		}
	}
}
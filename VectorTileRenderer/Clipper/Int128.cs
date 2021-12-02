using System;

namespace ClipperLib
{
	internal struct Int128
	{
		private long hi;

		private ulong lo;

		public Int128(long _lo)
		{
			this.lo = (ulong)_lo;
			if (_lo < (long)0)
			{
				this.hi = (long)-1;
				return;
			}
			this.hi = (long)0;
		}

		public Int128(long _hi, ulong _lo)
		{
			this.lo = _lo;
			this.hi = _hi;
		}

		public Int128(Int128 val)
		{
			this.hi = val.hi;
			this.lo = val.lo;
		}

		public override bool Equals(object obj)
		{
			if (obj == null || !(obj is Int128))
			{
				return false;
			}
			Int128 int128 = (Int128)obj;
			if (int128.hi != this.hi)
			{
				return false;
			}
			return int128.lo == this.lo;
		}

		public override int GetHashCode()
		{
			return this.hi.GetHashCode() ^ this.lo.GetHashCode();
		}

		public static Int128 Int128Mul(long lhs, long rhs)
		{
			bool negate = (lhs < 0) != (rhs < 0);
			if (lhs < 0) lhs = -lhs;
			if (rhs < 0) rhs = -rhs;
			UInt64 int1Hi = (UInt64)lhs >> 32;
			UInt64 int1Lo = (UInt64)lhs & 0xFFFFFFFF;
			UInt64 int2Hi = (UInt64)rhs >> 32;
			UInt64 int2Lo = (UInt64)rhs & 0xFFFFFFFF;

			//nb: see comments in clipper.pas
			UInt64 a = int1Hi * int2Hi;
			UInt64 b = int1Lo * int2Lo;
			UInt64 c = int1Hi * int2Lo + int1Lo * int2Hi;

			UInt64 lo;
			Int64 hi;
			hi = (Int64)(a + (c >> 32));

			unchecked { lo = (c << 32) + b; }
			if (lo < b) hi++;
			Int128 result = new Int128(hi, lo);
			return negate ? -result : result;
		}

		public bool IsNegative()
		{
			return this.hi < (long)0;
		}

		public static Int128 operator +(Int128 lhs, Int128 rhs)
		{
			lhs.hi += rhs.hi;
			lhs.lo += rhs.lo;
			if (lhs.lo < rhs.lo)
			{
				lhs.hi += (long)1;
			}
			return lhs;
		}

		//public static Int128 operator /(Int128 lhs, Int128 rhs)
		//{
		//	if (rhs.lo == (long)0 && rhs.hi == (long)0)
		//	{
		//		throw new ClipperException("Int128: divide by zero");
		//	}
		//	bool flag = rhs.hi < (long)0 != lhs.hi < (long)0;
		//	if (lhs.hi < (long)0)
		//	{
		//		lhs = -lhs;
		//	}
		//	if (rhs.hi < (long)0)
		//	{
		//		rhs = -rhs;
		//	}
		//	if (rhs >= lhs)
		//	{
		//		if (rhs == lhs)
		//		{
		//			return new Int128((long)1);
		//		}
		//		return new Int128((long)0);
		//	}
		//	Int128 int128 = new Int128((long)0);
		//	Int128 int1281 = new Int128((long)1);
		//	while (rhs.hi >= (long)0 && !(rhs > lhs))
		//	{
		//		rhs.hi <<= 1;
		//		if (rhs.lo < (long)0)
		//		{
		//			rhs.hi += (long)1;
		//		}
		//		rhs.lo <<= 1;
		//		int1281.hi <<= 1;
		//		if (int1281.lo < (long)0)
		//		{
		//			int1281.hi += (long)1;
		//		}
		//		int1281.lo <<= 1;
		//	}
		//	rhs.lo >>= 1;
		//	if ((rhs.hi & (long)1) == (long)1)
		//	{
		//		rhs.lo |= -9223372036854775808L;
		//	}
		//	rhs.hi >>= 1;
		//	int1281.lo >>= 1;
		//	if ((int1281.hi & (long)1) == (long)1)
		//	{
		//		int1281.lo |= -9223372036854775808L;
		//	}
		//	int1281.hi >>= 1;
		//	while (int1281.hi != (long)0 || int1281.lo != (long)0)
		//	{
		//		if (lhs >= rhs)
		//		{
		//			lhs -= rhs;
		//			int128.hi |= int1281.hi;
		//			int128.lo |= int1281.lo;
		//		}
		//		rhs.lo >>= 1;
		//		if ((rhs.hi & (long)1) == (long)1)
		//		{
		//			rhs.lo |= -9223372036854775808L;
		//		}
		//		rhs.hi >>= 1;
		//		int1281.lo >>= 1;
		//		if ((int1281.hi & (long)1) == (long)1)
		//		{
		//			int1281.lo |= -9223372036854775808L;
		//		}
		//		int1281.hi >>= 1;
		//	}
		//	if (!flag)
		//	{
		//		return int128;
		//	}
		//	return -int128;
		//}

		public static bool operator ==(Int128 val1, Int128 val2)
		{
			if ((object)val1 == (object)val2)
			{
				return true;
			}
			if ((object)val1 == null || (object)val2 == null)
			{
				return false;
			}
			if (val1.hi != val2.hi)
			{
				return false;
			}
			return val1.lo == val2.lo;
		}

		public static bool operator >(Int128 val1, Int128 val2)
		{
			if (val1.hi != val2.hi)
			{
				return val1.hi > val2.hi;
			}
			return val1.lo > val2.lo;
		}

		public static bool operator !=(Int128 val1, Int128 val2)
		{
			return !(val1 == val2);
		}

		public static bool operator <(Int128 val1, Int128 val2)
		{
			if (val1.hi != val2.hi)
			{
				return val1.hi < val2.hi;
			}
			return val1.lo < val2.lo;
		}

		public static Int128 operator -(Int128 lhs, Int128 rhs)
		{
			return lhs + -rhs;
		}

		public static Int128 operator -(Int128 val)
		{
			if (val.lo == (long)0)
			{
				return new Int128(-val.hi, (ulong)0);
			}
			return new Int128(~val.hi, ~val.lo + (long)1);
		}

		public double ToDouble()
		{
			if (this.hi >= (long)0)
			{
				return (double)((double)((float)this.lo) + (double)this.hi * 1.84467440737096E+19);
			}
			if (this.lo == (long)0)
			{
				return (double)this.hi * 1.84467440737096E+19;
			}
			return -(double)((double)((float)(~this.lo)) + (double)(~this.hi) * 1.84467440737096E+19);
		}
	}
}
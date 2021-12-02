using System;

namespace ClipperLib
{
	[Flags]
	internal enum Protects
	{
		ipNone,
		ipLeft,
		ipRight,
		ipBoth
	}
}
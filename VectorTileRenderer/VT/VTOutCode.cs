using System;

namespace VectorTileRenderer
{
    [Flags]
    public enum VTOutCode
    {
        Inside = 0,
        Left = 1,
        Right = 2,
        Bottom = 4,
        Top = 8
    }
}

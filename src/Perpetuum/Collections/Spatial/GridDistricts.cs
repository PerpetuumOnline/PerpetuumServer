using System;

namespace Perpetuum.Collections.Spatial
{
    [Flags]
    public enum GridDistricts : byte
    {
        Undefined = 0,
        Center = 1,
        Left = 1 << 1,
        Right = 1 << 2,
        Upper = 1 << 3,
        Lower = 1 << 4,
        LeftUpper = Left | Upper,
        LeftLower = Left | Lower,
        RightUpper = Right | Upper,
        RightLower = Right | Lower,
        All = Center | Left | Right | Upper | Lower | LeftLower | LeftUpper | RightLower | RightUpper
    }
}
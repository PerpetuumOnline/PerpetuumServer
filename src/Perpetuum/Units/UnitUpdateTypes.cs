using System;

namespace Perpetuum.Units
{
    [Flags]
    public enum UnitUpdateTypes
    {
        None              = 0x000,
        Position          = 0x001,
        Speed             = 0x002,
        Direction         = 0x004,
        Orientation       = 0x008,
        State             = 0x010,
        OptionalProperty  = 0x020,
        Lock              = 0x040,
        Detection         = 0x080,
        Stealth           = 0x100,
        TileChanged       = 0x200,

        Unit              = Position | Speed | Direction | Orientation | State | OptionalProperty,
        Visibility        = TileChanged | Lock | Detection | Stealth,
    }
}
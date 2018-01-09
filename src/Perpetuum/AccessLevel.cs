using System;

namespace Perpetuum
{
    /*
        AccessLevel values
        notDefined:0
        normal:2
        gameAdmin:6
        toolAdmin:14
        owner:30
    */

    /// <summary>
    /// Combined roles to control commands 
    /// </summary>
    [Flags]
    public enum AccessLevel : uint 
    {
        notDefined = 0,
        normal = 1 << 1,
        gameAdmin = 1 << 2 | normal,
        toolAdmin = 1 << 3 | gameAdmin,
        owner = 1<<4 | toolAdmin,

        allAdmin = toolAdmin | gameAdmin,

        admin = allAdmin,
    }
}
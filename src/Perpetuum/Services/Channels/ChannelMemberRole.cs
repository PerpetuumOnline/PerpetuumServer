using System;

namespace Perpetuum.Services.Channels
{
    [Flags]
    public enum ChannelMemberRole
    {
        Undefined = 0,
        Operator = 2, 
    }
}
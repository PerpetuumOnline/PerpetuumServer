

using System;

namespace Perpetuum.Accounting
{
    /// <summary>
    /// State of an account
    /// </summary>
    [Flags]
    public enum AccountState
    {
        notdefined = 0,
        normal = 1,
        banned = 2,
        trial = 4,
        beta = 8,
        vip = 16,
        investor = 32
    }

}

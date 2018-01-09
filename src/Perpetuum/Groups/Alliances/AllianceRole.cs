using System;

namespace Perpetuum.Groups.Alliances
{
    
    [Flags]
    public enum AllianceRole
    {
        NotDefined = 0,
        leader = 1,
        recruiter = 1 << 1,
        alliance_delegate = 1 << 2,
        roleAdmin = 1 << 3,
        profileAdmin = 1 << 4,
        standingOperator = 1 << 5
    }
    
}
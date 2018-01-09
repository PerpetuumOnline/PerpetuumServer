using System;

namespace Perpetuum.Services.Standing
{
    [Serializable]
    public class StandingInfo
    {
        public readonly long sourceEID;
        public readonly long targetEID;
        public readonly double standing;

        public StandingInfo(long sourceEID, long targetEID, double standing)
        {
            this.sourceEID = sourceEID;
            this.targetEID = targetEID;
            this.standing = standing;
        }
    }
}
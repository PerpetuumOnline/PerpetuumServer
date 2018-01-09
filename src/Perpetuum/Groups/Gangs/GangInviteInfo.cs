using System;
using Perpetuum.Accounting.Characters;
using Perpetuum.Timers;

namespace Perpetuum.Groups.Gangs
{
    public class GangInviteInfo
    {
        public readonly Guid gangGuid;
        public readonly Character sender;
        public readonly Character member;
        private readonly TimeTracker _timer = new TimeTracker(TimeSpan.FromMinutes(2));

        public GangInviteInfo(Guid gangGuid, Character sender, Character member)
        {
            this.gangGuid = gangGuid;
            this.sender = sender;
            this.member = member;
        }

        private bool _removable;

        public void ForceRemove()
        {
            _removable = true;
        }

        public bool IsExpired
        {
            get
            {
                if (_removable)
                    return true;
                
                return _timer.Expired;
            }
        }

        public void Update(TimeSpan time)
        {
            _timer.Update(time);
        }
    }
}
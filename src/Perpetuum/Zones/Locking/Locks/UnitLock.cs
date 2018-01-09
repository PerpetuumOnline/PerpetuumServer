using Perpetuum.Robots;
using Perpetuum.Units;

namespace Perpetuum.Zones.Locking.Locks
{
    public class UnitLock : Lock
    {
        public UnitLock(Robot owner) : base(owner)
        {
        }

        public override void AcceptVisitor(ILockVisitor visitor)
        {
            visitor.VisitUnitLock(this);
        }

        public Unit Target { get; set; }

        public override bool Equals(Lock other)
        {
            if (base.Equals(other))
                return true;

            var unitLockTarget = other as UnitLock;
            return unitLockTarget != null && Equals(Target.Eid, unitLockTarget.Target.Eid);
        }

        public override string ToString()
        {
            return "lockId:" + Id + " t:" + Target.InfoString + " s:" + State;
        }
    }
}
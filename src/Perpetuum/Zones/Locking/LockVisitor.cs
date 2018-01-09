using Perpetuum.Zones.Locking.Locks;

namespace Perpetuum.Zones.Locking
{
    public interface ILockVisitor
    {
        void VisitLock(Lock @lock);
        void VisitUnitLock(UnitLock unitLock);
        void VisitTerrainLock(TerrainLock terrainLock);
    }

    public class LockVisitor : ILockVisitor
    {
        public virtual void VisitLock(Lock @lock) { }

        public virtual void VisitUnitLock(UnitLock unitLock) { VisitLock(unitLock); }

        public virtual void VisitTerrainLock(TerrainLock terrainLock) { VisitLock(terrainLock); }
    }
}
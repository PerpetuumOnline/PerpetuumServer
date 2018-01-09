using Perpetuum.Zones.Locking.Locks;

namespace Perpetuum.Zones.Locking
{
    public class LockPacketBuilder : LockVisitor
    {
        private readonly BinaryStream _stream;
        private LockType _lockType;
        private readonly BinaryStream _target = new BinaryStream();

        private LockPacketBuilder(BinaryStream stream)
        {
            _stream = stream;
        }

        public override void VisitLock(Lock @lock)
        {
            _stream.AppendLong(@lock.Id);
            _stream.AppendByte((byte)_lockType);
            _stream.AppendByte((byte)@lock.State);
            _stream.AppendByte((byte)(@lock.Primary ? 1 : 0));
            _stream.AppendLong((long)@lock.Timer.Duration.TotalMilliseconds);
            _stream.AppendLong((long)@lock.Timer.Elapsed.TotalMilliseconds);
            _stream.AppendLong(@lock.Owner.Eid);
            _stream.AppendStream(_target);
        }

        public override void VisitUnitLock(UnitLock unitLock)
        {
            _lockType = LockType.Unit;
            _target.AppendLong(unitLock.Target.Eid);
            base.VisitUnitLock(unitLock);
        }

        public override void VisitTerrainLock(TerrainLock terrainLock)
        {
            _lockType = LockType.Terrain;
            _target.AppendInt(terrainLock.Location.intX);
            _target.AppendInt(terrainLock.Location.intY);
            _target.AppendInt(terrainLock.Location.intZ);
            base.VisitTerrainLock(terrainLock);
        }

        public static Packet BuildPacket(Lock @lock)
        {
            var packet = new Packet(ZoneCommand.LockState);
            AppendTo(@lock, packet);
            return packet;
        }

        public static void AppendTo(Lock @lock, BinaryStream stream)
        {
            var builder = new LockPacketBuilder(stream);
            @lock.AcceptVisitor(builder);
        }
    }
}
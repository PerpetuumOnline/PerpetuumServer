using Perpetuum.Builders;
using Perpetuum.Robots;
using Perpetuum.Zones.Terrains.Terraforming;

namespace Perpetuum.Zones.Locking.Locks
{
    public enum TerraformDirection
    {
        Undefined,
        Lower,
        Raise
    }

    public class TerrainLockParametersPacketBuilder : IBuilder<Packet>
    {
        private readonly TerrainLock _terrainLock;

        public TerrainLockParametersPacketBuilder(TerrainLock terrainLock)
        {
            _terrainLock = terrainLock;
        }

        public Packet Build()
        {
            var packet = new Packet(ZoneCommand.TerrainLockParameters);

            packet.AppendLong(_terrainLock.Id);
            packet.AppendByte((byte) _terrainLock.TerraformType);
            packet.AppendByte((byte) _terrainLock.TerraformDirection);
            packet.AppendByte((byte) _terrainLock.Radius);
            packet.AppendByte((byte) _terrainLock.Falloff);

            return packet;
        }
    }


    public class TerrainLock : Lock
    {
        private const int MAX_RADIUS = 5;

        
        private int _radius;

        public TerrainLock(Robot owner,Position location) : base(owner)
        {
            Location = location;
        }

        public TerraformType TerraformType { get; set; }
        public TerraformDirection TerraformDirection { get; set; }

        public int Radius
        {
            get { return _radius; }
            set { _radius = value.Clamp(0, MAX_RADIUS); }
        }

        public int Falloff { get; set; }

        public override void AcceptVisitor(ILockVisitor visitor)
        {
            visitor.VisitTerrainLock(this);
        }

        public Position Location { get; private set; }

        public override bool Equals(Lock other)
        {
            if (base.Equals(other))
                return true;

            var terrainLockTarget = other as TerrainLock;
            return terrainLockTarget != null && Equals(Location, terrainLockTarget.Location);
        }
    }
}
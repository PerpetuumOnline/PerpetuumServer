using System.Drawing;
using Perpetuum.EntityFramework;
using Perpetuum.Zones.Scanning.Ammos;
using Perpetuum.Zones.Terrains;
using Perpetuum.Zones.Terrains.Materials;

namespace Perpetuum.Zones.Scanning.Scanners
{
    public partial class Scanner : IEntityVisitor<DirectionalScannerAmmo>
    {
        private const int GOAL_RANGE = 5;
        private const double RANDOM_INTERVAL = 0.1;

        public void Visit(DirectionalScannerAmmo ammo)
        {
            var fromPosition = _player.CurrentPosition;

            var layer = _zone.Terrain.GetMineralLayerOrThrow(ammo.MaterialType);

            var nearestMineralPosition = Point.Empty;
            var nearestDist = int.MaxValue;

            foreach (var node in layer.Nodes)
            {
                var np = node.GetNearestMineralPosition(_player.CurrentPosition);
                var distance = np.SqrDistance(_player.CurrentPosition);
                if (distance >= nearestDist)
                    continue;

                nearestDist = distance;
                nearestMineralPosition = np;
            }

            var isInRange = fromPosition.IsInRangeOf2D(nearestMineralPosition, GOAL_RANGE);
            var direction = fromPosition.DirectionTo(nearestMineralPosition);
            direction = RandomizeDirection(direction);

            var packet = BuildPacket(ammo.MaterialType, fromPosition, nearestMineralPosition, direction, isInRange);
            _player.Session.SendPacket(packet);

            if (!isInRange)
                return;

            OnMineralScanned(MaterialProbeType.Directional, ammo.MaterialType);
        }

        private double RandomizeDirection(double direction)
        {
            var randomModifier = (FastRandom.NextDouble(-(1 - _module.ScanAccuracy), (1 - _module.ScanAccuracy))) * RANDOM_INTERVAL;
            direction += randomModifier;
            MathHelper.NormalizeDirection(ref direction);
            return direction;
        }

        private static Packet BuildPacket(MaterialType materialType, Position fromPosition, Point nearestMineralPosition, double direction, bool isInRange)
        {
            var packet = new Packet(ZoneCommand.ScanMineralDirectionalResult);
            packet.AppendInt((int) materialType);
            packet.AppendPoint(fromPosition);
            packet.AppendBool(nearestMineralPosition != Point.Empty);
            packet.AppendByte((byte) (direction*255));
            packet.AppendBool(isInRange);
            return packet;
        }

    }
}

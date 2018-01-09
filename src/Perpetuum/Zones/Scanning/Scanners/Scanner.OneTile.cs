using System.Drawing;
using System.Linq;
using Perpetuum.EntityFramework;
using Perpetuum.Zones.Scanning.Ammos;
using Perpetuum.Zones.Terrains.Materials.Minerals;

namespace Perpetuum.Zones.Scanning.Scanners
{
    public partial class Scanner : IEntityVisitor<OneTileScannerAmmo>
    {
        public void Visit(OneTileScannerAmmo ammo)
        {
            var packet = BuildScanOneTileResultPacket(_player.CurrentPosition);
            _player.Session.SendPacket(packet);

            //do mission
            OnMineralScanned(MaterialProbeType.OneTile);
        }

        private Packet BuildScanOneTileResultPacket(Point location)
        {
            var packet = new Packet(ZoneCommand.ScanOneTileResult);
            packet.AppendLong(_module.Eid); //module EID
            packet.AppendPoint(location);

            using (var bb = new BinaryStream())
            {
                var count = 0;

                foreach (var layer in _zone.Terrain.Materials.OfType<MineralLayer>())
                {
                    if (!layer.TryGetNode(location, out MineralNode node))
                        continue;

                    var amount = node.GetValue(location);
                    if ( amount <= 0 )
                        continue;

                    var m = _materialHelper.GetMaterialInfo(layer.Type);
                    var def = m.EntityDefault.Definition;
                    bb.AppendInt(def);
                    bb.AppendInt((int) amount);
                    count++;
                }

                packet.AppendByte((byte) count);
                packet.AppendStream(bb);
            }

            return packet;
        }
    }
}

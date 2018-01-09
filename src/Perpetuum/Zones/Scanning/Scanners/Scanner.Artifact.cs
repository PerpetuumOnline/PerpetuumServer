using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.Zones.Artifacts.Scanners;
using Perpetuum.Zones.Scanning.Ammos;

namespace Perpetuum.Zones.Scanning.Scanners
{
    public partial class Scanner : IEntityVisitor<ArtifactScannerAmmo>
    {
        public void Visit(ArtifactScannerAmmo ammo)
        {
            Task.Run(() =>
            {
                using (var scope = Db.CreateTransaction())
                {
                    var scanner = CreateArtifactScanner();
                    var scanPosition = _player.CurrentPosition;
                    var results = scanner.Scan(_player, ammo.ScanRange, _module.ScanAccuracy).ToArray();

                    Transaction.Current.OnCommited(() =>
                    {
                        _player.Session.SendPacket(BuildScanArtifactResultPacket(scanPosition,results));
                    });

                    scope.Complete();
                }
            });
        }

        private IArtifactScanner CreateArtifactScanner()
        {
            var factory = new ArtifactScannerFactory(_zone);
            return  factory.CreateArtifactScanner(_player);
        }

        private Packet BuildScanArtifactResultPacket(Position scanPosition,ArtifactScanResult[] results)
        {
            var packet = new Packet(ZoneCommand.ScanArtifactResult2);

            packet.AppendPoint(scanPosition);
            packet.AppendLong(_module.Eid);
            packet.AppendInt(results.Length);

            foreach (var result in results)
            {
                packet.AppendInt(result.scannedArtifact.Id);
                packet.AppendInt((int) result.scannedArtifact.Info.type);
                packet.AppendGuid(result.scannedArtifact.MissionGuid);
                packet.AppendPoint(result.estimatedPosition);
                packet.AppendDouble(result.radius);
            }

            return packet;
        }
    }
}

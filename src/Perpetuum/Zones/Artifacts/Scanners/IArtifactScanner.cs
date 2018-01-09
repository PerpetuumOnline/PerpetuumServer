using System.Collections.Generic;
using Perpetuum.Players;

namespace Perpetuum.Zones.Artifacts.Scanners
{
    public interface IArtifactScanner
    {
        IEnumerable<ArtifactScanResult> Scan(Player player, int scanRange, double scanAccuracy);
    }
}
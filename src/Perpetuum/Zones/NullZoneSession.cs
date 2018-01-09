using System;
using System.Collections.Generic;
using Perpetuum.Builders;
using Perpetuum.Zones.Beams;
using Perpetuum.Zones.Terrains;

namespace Perpetuum.Zones
{
    public class NullZoneSession : IZoneSession
    {
        public int Id
        {
            get { return 0; }
            
        }
        public void SendPacket(Packet packet) { }
        public void SendPacket(IBuilder<Packet> packetBuilder) { }

        public AccessLevel AccessLevel { get { return AccessLevel.notDefined; } }

        public DateTime DisconnectTime { get { return DateTime.Now; } }
        public TimeSpan InactiveTime { get { return TimeSpan.Zero; } }

        public void CancelLogout() { }
        public void ResetLogoutTimer() { }
        public void SendTerrainData() { }

        public void SendBeamIfVisible(Beam beam) {}
        public void SendBeam(IBuilder<Beam> builder) { }
        public void SendBeam(Beam beam) { }

        public void EnqueueLayerUpdates(IReadOnlyCollection<TerrainUpdateInfo> infos)
        {
        }

        public void Stop() { }
    }
}
using System;
using System.Collections.Generic;
using Perpetuum.Builders;
using Perpetuum.Zones.Beams;
using Perpetuum.Zones.Terrains;

namespace Perpetuum.Zones
{
    public interface IZoneSession
    {
        int Id { get; }

        void SendPacket(IBuilder<Packet> packetBuilder);
        void SendPacket(Packet packet);

        AccessLevel AccessLevel { get; }

        DateTime DisconnectTime { get; }
        TimeSpan InactiveTime { get;  }

        void CancelLogout();
        void ResetLogoutTimer();
        void SendTerrainData();

        void SendBeamIfVisible(Beam beam);
        void SendBeam(IBuilder<Beam> builder);
        void SendBeam(Beam beam);

        void EnqueueLayerUpdates(IReadOnlyCollection<TerrainUpdateInfo> infos);
        void Stop();
    }
}
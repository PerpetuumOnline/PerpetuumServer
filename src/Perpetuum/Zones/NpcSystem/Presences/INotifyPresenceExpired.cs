using System;

namespace Perpetuum.Zones.NpcSystem.Presences
{
    public interface INotifyPresenceExpired
    {
        event Action<Presence> PresenceExpired;
    }
}
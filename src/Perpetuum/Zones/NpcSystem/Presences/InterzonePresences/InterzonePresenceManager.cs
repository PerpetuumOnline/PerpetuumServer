using Perpetuum.Log;
using Perpetuum.Threading.Process;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Perpetuum.Zones.NpcSystem.Presences.InterzonePresences
{
    public class InterzonePresenceManager : Process
    {

        public delegate IInterzonePresenceManager Factory(IZoneManager zoneManager, PresenceFactory presenceFactory);

        private PresenceFactory _presenceFactory;
        private IInterzonePresenceConfigurationReader _configurationReader;
        private IEnumerable<InterzoneGroup> _groups = new List<InterzoneGroup>();
        private ImmutableList<Presence> _presences = ImmutableList<Presence>.Empty;

        public Lazy<IZoneManager> _zoneManager;

        public InterzonePresenceManager(Lazy<IZoneManager> zoneManager, PresenceFactory presenceFactory, IInterzonePresenceConfigurationReader configurationReader)
        {
            _presenceFactory = presenceFactory;
            _configurationReader = configurationReader;
            _zoneManager = zoneManager;
        }

        public override void Start()
        {
            base.Start();
            _groups = _configurationReader.GetAll();
            foreach (var group in _groups)
            {
                SpawnRandomPresenseOfGroup(group);
            }
        }

        public void SpawnRandomPresenseOfGroup(InterzoneGroup group)
        {
            var config = group.GetRandom();
            var zone = _zoneManager.Value.GetZone(config.ZoneID);
            var presence = _presenceFactory(zone, config);
            presence.LoadFlocks();
            AddPresence(presence);
        }

        public void AddPresence(Presence presence)
        {
            Logger.Info("Adding Presence: " + presence.Configuration.Name);
            ImmutableInterlocked.Update(ref _presences, p => p.Add(presence));

            if (presence is INotifyPresenceExpired notifier)
                notifier.PresenceExpired += OnPresenceExpired;
        }

        private void OnPresenceExpired(Presence presence)
        {
            ImmutableInterlocked.Update(ref _presences, p => p.Remove(presence));
            Logger.Info("Presence expired. " + presence.Configuration.Name);
            var group = _groups.First(g => g.id == presence.Configuration.InterzoneGroupId);
            SpawnRandomPresenseOfGroup(group);
        }

        public override void Update(TimeSpan time)
        {
            foreach (var presence in _presences)
            {
                presence.Update(time);
            }
        }

        public override void Stop()
        {
            base.Stop();
        }
    }
}

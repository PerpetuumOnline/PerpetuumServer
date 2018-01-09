using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Perpetuum.Log;
using Perpetuum.Threading.Process;

namespace Perpetuum.Zones.NpcSystem.Presences
{
    public class PresenceManager : Process,IPresenceManager
    {
        public delegate IPresenceManager Factory(IZone zone,PresenceFactory presenceFactory);

        private readonly IZone _zone;
        private readonly PresenceFactory _presenceFactory;
        private readonly IPresenceConfigurationReader _configurationReader;
        private ImmutableList<Presence> _presences = ImmutableList<Presence>.Empty;

        public PresenceManager(IZone zone,PresenceFactory presenceFactory,IPresenceConfigurationReader configurationReader)
        {
            _zone = zone;
            _presenceFactory = presenceFactory;
            _configurationReader = configurationReader;
        }

        public void LoadAll()
        {
            foreach (var configuration in _configurationReader.GetAll(_zone.Id))
            {
                var presence = CreatePresence(configuration);
                presence.LoadFlocks();
                AddPresence(presence);
            }
        }

        public IEnumerable<Presence> GetPresences()
        {
            return _presences;
        }

        public Presence CreatePresence(int presenceID)
        {
            var configuration = _configurationReader.Get(presenceID);
            if (configuration == null)
                return null;

            return CreatePresence(configuration);
        }

        private Presence CreatePresence(PresenceConfiguration configuration)
        {
            return _presenceFactory(_zone,configuration);
        }

        public void AddPresence(Presence presence)
        {
            ImmutableInterlocked.Update(ref _presences, p => p.Add(presence));

            if (presence is INotifyPresenceExpired notifier)
                notifier.PresenceExpired += OnPresenceExpired;
        }

        private void OnPresenceExpired(Presence presence)
        {
            ImmutableInterlocked.Update(ref _presences, p => p.Remove(presence));
            Logger.Info("Presence expired. " + presence.Configuration.name);
        }

        public override void Update(TimeSpan time)
        {
            foreach (var presence in _presences)
            {
                presence.Update(time);
            }
        }
    }
}
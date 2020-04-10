using Perpetuum.Threading.Process;
using System;
using System.Collections.Generic;

namespace Perpetuum.Zones.NpcSystem.Presences.InterzonePresences
{
    public class InterzonePresenceManager : Process
    {
        public Lazy<IZoneManager> _zoneManager;
        private readonly PresenceFactory _presenceFactory;
        private readonly IInterzonePresenceConfigurationReader _configurationReader;
        private IEnumerable<IInterzoneGroup> _groups = new List<IInterzoneGroup>();

        public InterzonePresenceManager(Lazy<IZoneManager> zoneManager, PresenceFactory presenceFactory, IInterzonePresenceConfigurationReader configurationReader)
        {
            _presenceFactory = presenceFactory;
            _configurationReader = configurationReader;
            _zoneManager = zoneManager;
        }

        public override void Start()
        {
            _groups = _configurationReader.GetAll();
            foreach (var group in _groups)
            {
                group.Init(_zoneManager.Value, _presenceFactory);
            }
            base.Start();
        }

        public override void Update(TimeSpan time)
        {
            foreach (var group in _groups)
            {
                group.Update(time);
            }
        }

        public override void Stop()
        {
            base.Stop();
        }
    }
}

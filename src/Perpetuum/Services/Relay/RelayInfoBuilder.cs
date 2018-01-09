using System.Collections.Generic;
using System.Linq;
using Perpetuum.Builders;
using Perpetuum.Services.Sessions;

namespace Perpetuum.Services.Relay
{
    public class RelayInfo
    {
        public RelayState state;
        public string name;
        public int usersCount;
        public int maxUsers;

        public Dictionary<string,object> ToDictionary()
        {
            return new Dictionary<string,object>
            {
                {k.state,(int)state},
                {k.name,name},
                {k.users,usersCount},
                {k.maxUsers,maxUsers},
            };
        }
    }

    public class RelayInfoBuilder : IBuilder<RelayInfo>
    {
        private readonly GlobalConfiguration _globalConfiguration;
        private readonly IRelayStateService _relayStateService;
        private readonly ISessionManager _sessionManager;

        public delegate RelayInfoBuilder Factory();

        public RelayInfoBuilder(GlobalConfiguration globalConfiguration, IRelayStateService relayStateService, ISessionManager sessionManager)
        {
            _globalConfiguration = globalConfiguration;
            _relayStateService = relayStateService;
            _sessionManager = sessionManager;
        }

        public RelayInfo Build()
        {
            var info = new RelayInfo
            {
                state = _relayStateService.State,
                name = _globalConfiguration.RelayName,
                usersCount = _sessionManager.Sessions.Count(),
                maxUsers = _sessionManager.MaxSessions
            };
            return info;
        }
    }
}
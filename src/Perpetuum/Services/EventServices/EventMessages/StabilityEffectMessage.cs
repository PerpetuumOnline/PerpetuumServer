using System.Collections.Generic;
using Perpetuum.Groups.Corporations;
using Perpetuum.Players;
using Perpetuum.Zones.Intrusion;

namespace Perpetuum.Services.EventServices.EventMessages
{
    /// <summary>
    /// An EventMessage of an event to alter Outpost stability (SAP)
    /// </summary>
    public class StabilityAffectingEvent : EventMessage
    {
        private Player _player;
        private IList<Player> _particpants = new List<Player>();
        public Outpost Outpost { get; }
        public bool OverrideRelations { get; }
        public int StabilityChange { get; }
        public int Definition { get; }
        public long? Eid { get; }

        public StabilityAffectingEvent(Outpost outpost, Player winner, int def, long? eid, int sapPoints, IList<Player> participants = null, bool overrideRelations = false)
        {
            Outpost = outpost;
            _player = winner;
            StabilityChange = sapPoints;
            Definition = def;
            Eid = eid;
            if (participants != null)
            {
                _particpants = participants;
            }
            
            OverrideRelations = overrideRelations;
        }

        public bool IsSystemGenerated()
        {
            return _player == null;
        }

        public IList<Player> GetPlayers()
        {
            return _particpants;
        }

        [CanBeNull]
        public Corporation GetWinnerCorporation()
        {
            if (IsSystemGenerated())
                return Corporation.GetByName("syndicate_police_central");
            return Corporation.Get(_player.CorporationEid);
        }
    }
}

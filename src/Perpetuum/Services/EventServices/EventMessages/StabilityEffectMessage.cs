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
        public Corporation Winner { get; private set; }
        public Outpost Outpost { get; private set; }
        public bool OverrideRelations { get; private set; }
        public int StabilityChange { get; private set; }
        public int Definition { get; private set; }
        public long? Eid { get; private set; }
        private List<Player> _participants = new List<Player>();
        public List<Player> Participants
        {
            get
            {
                _participants.RemoveAll(p => p == null);
                return _participants;
            }
            private set
            {
                _participants = value;
            }
        }

        public static StabilityAffectBuilder Builder()
        {
            return new StabilityAffectBuilder();
        }

        public bool IsSystemGenerated()
        {
            return Winner == null;
        }

        [CanBeNull]
        public Corporation GetWinnerCorporation()
        {
            if (IsSystemGenerated())
                return Corporation.GetByName("syndicate_police_central");
            return Winner;
        }

        public class StabilityAffectBuilder
        {
            private Outpost _outpost;
            private Corporation _winnerCorp;
            private int _definition;
            private long? _entityId;
            private int _sapPoints;
            private List<Player> _participants = new List<Player>();
            private bool _overrideRelations = false;

            public StabilityAffectingEvent Build()
            {
                var s = new StabilityAffectingEvent
                {
                    Outpost = _outpost,
                    Winner = _winnerCorp,
                    Participants = _participants,
                    Definition = _definition,
                    Eid = _entityId,
                    StabilityChange = _sapPoints,
                    OverrideRelations = _overrideRelations
                };
                return s;
            }

            public StabilityAffectBuilder WithOutpost(Outpost outpost)
            {
                _outpost = outpost;
                return this;
            }
            public StabilityAffectBuilder AddParticipant(Player player)
            {
                _participants.Add(player);
                return this;
            }
            public StabilityAffectBuilder AddParticipants(IList<Player> players)
            {
                _participants.AddMany(players);
                return this;
            }
            public StabilityAffectBuilder WithWinnerCorp(long corpEid)
            {
                _winnerCorp = Corporation.Get(corpEid);
                return this;
            }
            public StabilityAffectBuilder WithSapDefinition(int definition)
            {
                _definition = definition;
                return this;
            }
            public StabilityAffectBuilder WithSapEntityID(long eid)
            {
                _entityId = eid;
                return this;
            }
            public StabilityAffectBuilder WithPoints(int sapPoints)
            {
                _sapPoints = sapPoints;
                return this;
            }
            public StabilityAffectBuilder WithOverrideRelations(bool isOverride)
            {
                _overrideRelations = isOverride;
                return this;
            }
        }
    }
}

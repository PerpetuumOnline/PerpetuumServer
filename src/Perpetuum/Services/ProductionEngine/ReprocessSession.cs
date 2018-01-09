using System.Collections.Generic;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Containers;
using Perpetuum.Items;

namespace Perpetuum.Services.ProductionEngine
{
    public class ReprocessSession
    {
        private readonly ReprocessSessionMember.Factory _sessionMemberFactory;
        private readonly List<ReprocessSessionMember> _sessionMembers = new List<ReprocessSessionMember>();

        public delegate ReprocessSession Factory();

        public ReprocessSession(ReprocessSessionMember.Factory sessionMemberFactory)
        {
            _sessionMemberFactory = sessionMemberFactory;
        }

        public void WriteSessionToSql(Container container, Dictionary<int, int> randomComponentResults)
        {
            foreach (var reprocessSessionMember in _sessionMembers)
            {
                reprocessSessionMember.WriteToSql(container, randomComponentResults);
            }
        }

        public Dictionary<string, object> GetQueryDictionary()
        {
            var counter = 0;

            return _sessionMembers
                .Select(reprocessSessionMember => reprocessSessionMember.ToDictionary())
                .ToDictionary<Dictionary<string, object>, string, object>(oneResult => "r" + counter++, oneResult => oneResult);
        }


        public void AddMember(Item targetItem, double materialEfficiency, Character character)
        {
            var reprocessSessionMember = _sessionMemberFactory();

            reprocessSessionMember.Init(targetItem, materialEfficiency, character);

            _sessionMembers.Add(reprocessSessionMember);
        }
    }
}
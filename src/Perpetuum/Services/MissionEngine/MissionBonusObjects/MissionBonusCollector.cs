using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Perpetuum.Services.MissionEngine.Missions;

namespace Perpetuum.Services.MissionEngine.MissionBonusObjects
{

    public class MissionBonusCollector
    {
        private readonly ConcurrentDictionary<long, MissionBonus> _bonuses = new ConcurrentDictionary<long, MissionBonus>();

        public IEnumerable<MissionBonus> ActiveBonuses()
        {
            return _bonuses.Values;
        }


        public bool IsEmpty
        {
            get { return !_bonuses.Any(); }
        }

        public void AddBonus(MissionBonus missionBonus)
        {
            _bonuses.AddOrUpdate(missionBonus.Key, v => missionBonus, (k, v) => missionBonus);
        }

        public void RemoveBonus(MissionBonus missionBonus)
        {
            _bonuses.Remove(missionBonus.Key);
        }

        public bool GetBonusWithConditions(MissionCategory missionCategory, int missionLevel, MissionAgent agent, out MissionBonus missionBonus)
        {
            var key = MissionBonus.GetKey(missionCategory, missionLevel, agent.id);
            return _bonuses.TryGetValue(key, out missionBonus);
        }

        public Dictionary<string, object> ToDictionary()
        {
            var counter = 0;
            var result = new Dictionary<string, object>();
            foreach (var missionBonus in ActiveBonuses())
            {
                var oneEntry = missionBonus.ToDictionary();

                result.Add("b" + counter++, oneEntry);

            }
            return result;

        }

    }
}

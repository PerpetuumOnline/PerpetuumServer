using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;

namespace Perpetuum.Services.MissionEngine.MissionProcessorObjects
{
    public partial class MissionProcessor
    {
        private readonly ConcurrentDictionary<int, List<int>> _finishedMissions = new ConcurrentDictionary<int, List<int>>();

        private Dictionary<int, DateTime> GetFinishedPeriodicMissions(Character character)
        {
            var periodicMissionIds = _missionDataCache.GetPeriodicMissionIds().ToArray();
            var maxMinutes = _missionDataCache.GetMaximumPeriod();

            if (periodicMissionIds.IsNullOrEmpty() || maxMinutes == 0)
            {
                return new Dictionary<int, DateTime>();
            }

            var querystring = $"SELECT MAX(finished),missionid FROM missionlog WHERE characterid=@characterID and finished IS NOT NULL AND missionid IN ({periodicMissionIds.ArrayToString()}) and finished>@finishLimit GROUP BY missionID";

            var records = Db.Query().CommandText(querystring)
                .SetParameter("@characterID", character.Id)
                .SetParameter("@finishLimit", DateTime.Now.AddMinutes(-1*maxMinutes))
                .Execute();

            return records.ToDictionary(record => record.GetValue<int>(1), record => record.GetValue<DateTime>(0));
        }


        public void ResetFinishedMissionsOnServer()
        {
            _finishedMissions.Clear();
        }

        public void FinishedMissionsClearCache(Character character)
        {
            List<int> lista;
            _finishedMissions.TryRemove(character.Id, out lista);
        }

        public void AddToFinishedMissions(Character character, int missionId)
        {
            var list = _finishedMissions.GetOrAdd(character.Id, GetSuccessfullyFinishedMissions(character));

            if (!list.Contains(missionId))
            {
                list.Add(missionId);
            }

            _finishedMissions.AddOrUpdate(character.Id, list, (id, lista) => list);
        }

        private ErrorCodes GetFinishedAndLastMissions(Character character, out List<int> missionsDone, out Dictionary<int, DateTime> periodicMissions)
        {
            //load everything from sql
            missionsDone = GetSuccessfullyFinishedMissions(character);

            periodicMissions = GetFinishedPeriodicMissions(character);

            return ErrorCodes.NoError;
        }


        private List<int> GetSuccessfullyFinishedMissions(Character character)
        {
            List<int> missionsDone;

            //try to get it from the cache
            if (!_finishedMissions.TryGetValue(character.Id, out missionsDone))
            {
                //load from sql
                missionsDone = Db.Query().CommandText("select distinct missionID from missionlog where characterid=@characterID and finished is not NULL and succeeded=1")
                    .SetParameter("@characterID", character.Id)
                    .Execute().Select(m => m.GetValue<int>(0)).ToList();

                _finishedMissions.AddOrUpdate(character.Id, missionsDone, (id, list) => missionsDone); //add it to the ram cache
            }

            return missionsDone;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Log;
using Perpetuum.Services.MissionEngine.MissionProcessorObjects;
using Perpetuum.Services.MissionEngine.Missions;
using Perpetuum.Services.MissionEngine.MissionStructures;

namespace Perpetuum.Services.MissionEngine
{
    internal class OneLocationTest
    {
        private readonly MissionProcessor _missionProcessor;
        private readonly List<Position> _terminalsOnZones;
        public OneLocationTest(MissionProcessor missionProcessor, List<Position> terminalsOnZones)
        {
            _missionProcessor = missionProcessor;
            _terminalsOnZones = terminalsOnZones;
        }

        public void TestOne(MissionLocation location, Mission mission, Character testCharacter, int missionLevel, int maxAttempts = 100, bool writeResult = true)
        {
            var rewardCollector = 0.0;

            var wasException = false;
            var structureHashList = new List<long>();
            //Logger.Info(" location " + location.id);

            int successCount = 0;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                MissionInProgress missionInProgress = null;
                bool success = false;
                try
                {
                    success =
                        _missionProcessor.MissionAdministrator.TryCreateMission(testCharacter, false, mission, location, missionLevel, out missionInProgress, true);
                }
                catch (Exception ex)
                {
                    //this mission has some content/config problem
                    Logger.Error("exception occured in mission resolve: " + mission);
                    Logger.Exception(ex);
                    wasException = true;
                    break;
                }

                if (success)
                {
                    var sHash = missionInProgress.GenerateStructureHash();
                    if (sHash > 0)
                        structureHashList.Add(sHash);

                    successCount++;

                    missionInProgress.GenerateSuccessInfoForTest(_terminalsOnZones);

                    double rewardSum;
                    double distanceReward;
                    double difficultyReward;
                    double rewardByTargets;
                    double riskCompensation;
                    double zoneFactor;
                    missionInProgress.GetFinalReward(out rewardSum, out distanceReward, out difficultyReward, out rewardByTargets, out riskCompensation, out zoneFactor);
                    rewardCollector += rewardSum;

                    if (writeResult)
                    {
                        missionInProgress.WriteSuccessLogAllTargets();
                    }

                }
               

            }

            if (!wasException)
            {
                var rewardAverage = 0;
                if (successCount > 0)
                {
                    rewardAverage = (int)(rewardCollector / (double)successCount);    
                }
                else
                {
                    Logger.Error("100% failure: " + location + " " + mission);
                }

                var uniqueHash = structureHashList.Distinct().Count();
                Logger.Info("success:" + successCount + " unique:" + uniqueHash);

                Logger.Info("paid reward " + rewardAverage);
                if (writeResult)
                {
                    //make it blocking, on purpose
                    MissionResolveInfo.InsertToDb(mission, location, maxAttempts, successCount, uniqueHash, rewardAverage);
                }

            }
            else
            {
                Logger.Error("--------------------------");
                Logger.Error("--------------------------");
                Logger.Error("exception:" +  location + " " + mission);
                Logger.Error("--------------------------");
                Logger.Error("--------------------------");


            }

        }

    }
}
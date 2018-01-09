using System.Threading.Tasks;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Log;
using Perpetuum.Services.MissionEngine;
using Perpetuum.Services.MissionEngine.MissionProcessorObjects;

namespace Perpetuum.RequestHandlers.Missions
{
    public class MissionResolveTest : IRequestHandler
    {
        private readonly MissionProcessor _missionProcessor;

        public MissionResolveTest(MissionProcessor missionProcessor)
        {
            _missionProcessor = missionProcessor;
        }


        public void HandleRequest(IRequest request)
        {
            var zoneId = request.Data.GetOrDefault(k.zone, -1);

            if (zoneId == -1)
            {
                Logger.Error("No #zone defined.");
                return;
            }

            var characterId = request.Data.GetOrDefault<int>(k.characterID);

            var testCharacter = characterId == 0 ? null : Character.Get(characterId);

            var missionLevel = request.Data.GetOrDefault(k.level, 4);

            var resolveTest = new MissionResolveTester(_missionProcessor,zoneId);

            var displayOnly = request.Data.GetOrDefault<int>("display") == 1;
            var writeResult = !displayOnly;

            var maxAttempts = request.Data.GetOrDefault("attempts", 100);

            var singleLocation = request.Data.GetOrDefault<int>("single") == 1;

            if (writeResult)
            {
                var deletedMissionResolveRecords =
                    Db.Query().CommandText("DELETE dbo.missiontolocation WHERE locationid IN (SELECT id FROM dbo.missionlocations WHERE zoneid=@zoneId)")
                        .SetParameter("@zoneId", zoneId)
                        .ExecuteNonQuery();

                Logger.Info(deletedMissionResolveRecords + " deleted from missiontolocation.");

                var deletedMissionTargetsLog =
                    Db.Query().CommandText("delete missiontargetslog where zoneid=@zoneId").SetParameter("@zoneId", zoneId).ExecuteNonQuery();

                Logger.Info(deletedMissionTargetsLog + " deleted from missiontargetslog.");
            }

            var mainTask = Task.Run(()=>resolveTest.RunTestParallel(testCharacter, missionLevel,maxAttempts, writeResult,singleLocation))
                .ContinueWith((t)=>
            {
                MissionResolveTester.isTestMode = false;
                Message.Builder.FromRequest(request).WithOk().Send();
            });

            mainTask.Wait();
        }
    }
}

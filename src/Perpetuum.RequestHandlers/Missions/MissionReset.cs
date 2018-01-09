using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.MissionEngine.AdministratorObjects;
using Perpetuum.Services.MissionEngine.MissionProcessorObjects;

namespace Perpetuum.RequestHandlers.Missions
{
    /// <summary>
    /// Resets missions for a character or the request sender
    /// Clears missionlog, kills running missions
    /// </summary>
    public class MissionReset : IRequestHandler
    {
        private readonly MissionProcessor _missionProcessor;

        public MissionReset(MissionProcessor missionProcessor)
        {
            _missionProcessor = missionProcessor;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = Character.Get(request.Data.GetOrDefault<int>(k.characterID)) ?? request.Session.Character;

                Db.Query().CommandText("delete from missiontargetsarchive where characterid=@characterID; DELETE  FROM missionlog where characterid=@characterID")
                    .SetParameter("@characterID", character.Id)
                    .ExecuteNonQuery();

                _missionProcessor.FinishedMissionsClearCache(character);

                if (_missionProcessor.MissionAdministrator.GetMissionInProgressCollector(character,out MissionInProgressCollector mcollector))
                {
                    mcollector.Reset();
                }

                Message.Builder.FromRequest(request).WithOk().Send();
                
                scope.Complete();
            }
        }
    }
}
using System.Collections.Generic;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Standing;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationSetMembersNeutral : IRequestHandler
    {
        private readonly IStandingHandler _standingHandler;

        public CorporationSetMembersNeutral(IStandingHandler standingHandler)
        {
            _standingHandler = standingHandler;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;

                var corporation = character.GetPrivateCorporationOrThrow();

                var counter = 0;
                var standingDict = new Dictionary<string, object>();

                foreach (var member in corporation.GetCharacterMembers())
                {
                    var memberEid = member.Eid;

                    _standingHandler.SetStanding(character.Eid, memberEid, 0.0);

                    var oneEntry = new Dictionary<string, object>
                    {
                        {k.targetEID, memberEid},
                        {k.standing, 0.0},
                    };

                    standingDict.Add("s" + counter++, oneEntry);
                }

                var result = new Dictionary<string, object>
                {
                    {k.sourceEID, character.Eid},
                    {k.standing, standingDict},
                    {k.characterEID, character.Eid}
                };

                Message.Builder.SetCommand(Commands.StandingList)
                    .WithData(result)
                    .WrapToResult()
                    .ToClient(request.Session)
                    .Send();
                
                scope.Complete();
            }
        }
    }
}
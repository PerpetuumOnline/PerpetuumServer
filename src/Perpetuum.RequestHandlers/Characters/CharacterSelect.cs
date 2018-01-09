using System;
using System.Collections.Generic;
using System.Transactions;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Characters
{
    public class CharacterSelect : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var session = request.Session;
            if (!session.IsAuthenticated)
                throw new PerpetuumException(ErrorCodes.NotSignedIn);

            var character = Character.Get(request.Data.GetOrDefault<int>(k.characterID));
            if (character.AccountId != request.Session.AccountId)
                throw new PerpetuumException(ErrorCodes.AccessDenied);

            if (character.IsOffensiveNick)
                throw PerpetuumException.Create(ErrorCodes.OffensiveNick).SetData("characterID", character.Id);

            var isDocked = character.IsDocked;
            var zone = character.GetCurrentZone();

            if (!isDocked)
            {
                if (zone == null || character.ZonePosition == null || character.ActiveRobotEid == 0L)
                {
                    isDocked = true;
                }
            }

            using (var scope = Db.CreateTransaction())
            {
                character.Nick = character.Nick.Replace("_renamed_", "");
                character.LastUsed = DateTime.Now;
                character.IsDocked = isDocked;
                character.Language = request.Data.GetOrDefault<int>(k.language);
                character.IsOnline = true;

                if (isDocked)
                {
                    character.ZoneId = null;
                    character.ZonePosition = null;
                    character.GetCurrentDockingBase()?.JoinChannel(character);
                }

                var corporation = character.GetCorporation();
                var alliance = character.GetAlliance();

                Transaction.Current.OnCommited(() =>
                {
                    session.SelectCharacter(character);

                    if (isDocked)
                    {
                        var result = new Dictionary<string, object>
                        {
                            {k.characterID, character.Id},
                            {k.rootEID,character.Eid},
                            {k.corporationEID, corporation.Eid},
                            {k.allianceEID,alliance?.Eid ?? 0L}
                        };

                        Message.Builder.FromRequest(request).WithData(result).Send();
                    }
                    else
                    {
                        zone?.Enter(character, Commands.CharacterSelect);
                    }
                });

                scope.Complete();
            }
        }
    }
}
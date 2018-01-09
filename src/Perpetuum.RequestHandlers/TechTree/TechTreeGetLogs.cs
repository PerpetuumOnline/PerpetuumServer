using System;
using System.Collections.Generic;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;
using Perpetuum.Services.TechTree;

namespace Perpetuum.RequestHandlers.TechTree
{
    public class TechTreeGetLogs : TechTreeRequestHandler
    {
        public override void HandleRequest(IRequest request)
        {
            var offset = TimeSpan.FromDays(request.Data.GetOrDefault<int>(k.offset));
            var duration = TimeSpan.FromDays(request.Data.GetOrDefault<int>(k.duration));

            var from = DateTime.Now - offset;
            var to = from - duration;
            var forCorporation = request.Data.GetOrDefault<int>(k.forCorporation).ToBool();

            TechTreeLogger techTreeLogger;

            var character = request.Session.Character;
            if (forCorporation)
            {
                Corporation.GetRoleFromSql(character).HasRole(PresetCorporationRoles.CAN_LIST_TECHTREE).ThrowIfFalse(ErrorCodes.TechTreeAccessDenied);
                techTreeLogger = new CorporationTechTreeLogger(character.CorporationEid);
            }
            else
            {
                techTreeLogger = new CharacterTechTreeLogger(character);
            }

            var result = new Dictionary<string, object>
            {
                {k.forCorporation, forCorporation},
                {k.entries, techTreeLogger.GetAll(from, to).ToDictionary("e", e => e.ToDictionary())}
            };

            Message.Builder.FromRequest(request).WithData(result).Send();
        }
    }
}
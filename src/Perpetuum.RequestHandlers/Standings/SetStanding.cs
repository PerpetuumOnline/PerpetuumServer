using System.Collections.Generic;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Groups.Alliances;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Standing;

namespace Perpetuum.RequestHandlers.Standings
{
    public class SetStanding : IRequestHandler
    {
        private readonly IStandingHandler _standingHandler;

        public SetStanding(IStandingHandler standingHandler)
        {
            _standingHandler = standingHandler;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var source = request.Data.GetOrDefault<long>(k.source);
                var target = request.Data.GetOrDefault<long>(k.target);
                var character = request.Session.Character;

                source.ThrowIfEqual(target, ErrorCodes.WTFErrorMedicalAttentionSuggested);

                var standingValue = request.Data.GetOrDefault<double>(k.standing);

                var sourceEntity = Entity.Repository.LoadOrThrow(source);
                var targetEntity = Entity.Repository.LoadOrThrow(target);


                //standing can only be set to a characterEID or corporation or alliance
                (!targetEntity.IsCategory(CategoryFlags.cf_player) &&
                 !targetEntity.IsCategory(CategoryFlags.cf_corporation) &&
                 !targetEntity.IsCategory(CategoryFlags.cf_alliance)).ThrowIfTrue(ErrorCodes.AccessDenied);

                if (targetEntity is Corporation)
                {
                    //only private corp
                    targetEntity.ThrowIfNotType<PrivateCorporation>(ErrorCodes.AccessDenied);
                }

                if (targetEntity is Alliance)
                {
                    //only private alliance
                    targetEntity.ThrowIfNotType<PrivateAlliance>(ErrorCodes.AccessDenied);
                }

                //standing can only be set to a characterEID or corporation or alliance
                (!sourceEntity.IsCategory(CategoryFlags.cf_player) &&
                 !sourceEntity.IsCategory(CategoryFlags.cf_corporation) &&
                 !sourceEntity.IsCategory(CategoryFlags.cf_alliance)).ThrowIfTrue(ErrorCodes.AccessDenied);

                var sourceCorporation = sourceEntity as PrivateCorporation;
                if (sourceCorporation != null)
                {
                    //only private corp
                    character.CorporationEid.ThrowIfNotEqual(sourceCorporation.Eid, ErrorCodes.AccessDenied);
                    sourceCorporation.IsAnyRole(character, CorporationRole.CEO, CorporationRole.DeputyCEO, CorporationRole.PRManager).ThrowIfFalse(ErrorCodes.InsufficientPrivileges);
                }

                if (sourceEntity is Alliance)
                {
                    //only private alliance
                    sourceEntity.ThrowIfNotType<PrivateAlliance>(ErrorCodes.AccessDenied);
                }

                //character only to character
                (sourceEntity.IsCategory(CategoryFlags.cf_player) && !targetEntity.IsCategory(CategoryFlags.cf_player)).ThrowIfTrue(ErrorCodes.AccessDenied);

                //corporation only to corporation
                (sourceEntity.IsCategory(CategoryFlags.cf_corporation) && !targetEntity.IsCategory(CategoryFlags.cf_corporation)).ThrowIfTrue(ErrorCodes.AccessDenied);

                //alliance only to alliance
                (sourceEntity.IsCategory(CategoryFlags.cf_alliance) && !targetEntity.IsCategory(CategoryFlags.cf_alliance)).ThrowIfTrue(ErrorCodes.AccessDenied);

                //finally set the standing
                _standingHandler.SetStanding(source, target, standingValue);

                if (sourceCorporation != null)
                {
                    Message.Builder.SetCommand(request.Command)
                        .WithData(new Dictionary<string, object> { { k.result, request.Data } })
                        .ToCorporation(sourceCorporation)
                        .Send();

                    var targetCorporation = targetEntity as PrivateCorporation;
                    if (targetCorporation != null)
                    {
                        var result = new Dictionary<string, object>
                        {
                            {k.sourceEID, source},
                            {k.standing, new Dictionary<string, object>
                            {
                                {"s1", new Dictionary<string, object>
                                    {
                                        {k.targetEID, target},
                                        {k.standing, standingValue}
                                    }
                                }
                            }}
                        };

                        //inform target corporation members
                        const CorporationRole roleMask = CorporationRole.CEO | CorporationRole.DeputyCEO | CorporationRole.PRManager;

                        Message.Builder.SetCommand(Commands.StandingSetOnMyCorporation)
                            .WithData(result)
                            .WrapToResult()
                            .ToCorporation(targetCorporation, roleMask)
                            .Send();
                    }

                    return;
                }

                Message.Builder.FromRequest(request)
                    .WithData(request.Data)
                    .WrapToResult()
                    .WithEmpty()
                    .Send();
                
                scope.Complete();
            }
        }
    }
}
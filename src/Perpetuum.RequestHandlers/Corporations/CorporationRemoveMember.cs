using System.Collections.Generic;
using System.Transactions;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Channels;
using Perpetuum.Zones;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationRemoveMember : IRequestHandler
    {
        private readonly ICorporationManager _corporationManager;
        private readonly IChannelManager _channelManager;
        private readonly IZoneManager _zoneManager;
        private readonly ICharacterProfileRepository _characterProfiles;

        public CorporationRemoveMember(ICorporationManager corporationManager,IChannelManager channelManager,IZoneManager zoneManager,ICharacterProfileRepository characterProfiles)
        {
            _corporationManager = corporationManager;
            _channelManager = channelManager;
            _zoneManager = zoneManager;
            _characterProfiles = characterProfiles;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var member = Character.Get(request.Data.GetOrDefault<int>(k.memberID));

                if (character == member)
                    return;

                //a member was kicked, while he was leaving the corp
                _corporationManager.CleanUpCharacterLeave(member);

                var corporation = character.GetPrivateCorporationOrThrow();
                var issuerRole = corporation.GetMemberRole(character);

                issuerRole.IsAnyRole(CorporationRole.CEO, CorporationRole.HRManager, CorporationRole.DeputyCEO).ThrowIfFalse(ErrorCodes.InsufficientPrivileges);

                var issuerCEO = issuerRole.IsAnyRole(CorporationRole.CEO);

                var targetRole = corporation.GetMemberRole(member);

                //if the issuer is NOT ceo then we have some rules
                if (!issuerCEO)
                {
                    //no ceo, no deputyCEO
                    targetRole.IsAnyRole(CorporationRole.CEO, CorporationRole.DeputyCEO).ThrowIfTrue(ErrorCodes.InsufficientPrivileges);
                }

                //----------------------------------------------
                //%%% ALLIANCE stuff es egyebek!!!!!!!! %%%
                //ez itt egy hack
                targetRole = CorporationRole.NotDefined;
                //-----------------------------------------------

                //alliance board membership and all the rest is protected by this branch
                targetRole.ThrowIfNotEqual(CorporationRole.NotDefined, ErrorCodes.MemberHasRolesError);

                var freelancerCorporation = DefaultCorporation.GetFreelancerCorporation();
                freelancerCorporation.AddMember(member, CorporationRole.NotDefined, corporation);

                corporation.RemoveMember(member);

                freelancerCorporation.Save();

                Transaction.Current.OnCommited(() =>
                {
                    if (_characterProfiles is CachedCharacterProfileRepository c)
                        c.Remove(member.Id);

                    _channelManager.LeaveChannel(corporation.ChannelName, member);
                    _channelManager.JoinChannel(freelancerCorporation.ChannelName, member, CorporationRole.NotDefined);
                    _corporationManager.InformCorporationMemberTransferred(corporation, freelancerCorporation, member, character);

                    member.GetPlayerRobotFromZone()?.UpdateCorporationOnZone(freelancerCorporation.Eid);

                    //inform the zones
                    var info = new Dictionary<string, object>
                    {
                        {k.from, corporation.Eid},
                        {k.to, freelancerCorporation.Eid},
                        {k.characterID, member.Id},
                    };

                    _zoneManager.Zones.ForEach(z => z.UpdateCorporation(CorporationCommand.TransferMember, info));

                });

                Message.Builder.FromRequest(request).WithOk().Send();
                
                scope.Complete();
            }
        }
    }
}
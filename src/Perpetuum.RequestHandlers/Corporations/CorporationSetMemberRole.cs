using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Channels;
using Perpetuum.Units.DockingBases;
using Perpetuum.Zones.PBS.DockingBases;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationSetMemberRole : IRequestHandler
    {
        private readonly ICorporationManager _corporationManager;
        private readonly IChannelManager _channelManager;
        private readonly DockingBaseHelper _dockingBaseHelper;

        public CorporationSetMemberRole(ICorporationManager corporationManager,IChannelManager channelManager,DockingBaseHelper dockingBaseHelper)
        {
            _corporationManager = corporationManager;
            _channelManager = channelManager;
            _dockingBaseHelper = dockingBaseHelper;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var member = Character.Get(request.Data.GetOrDefault<int>(k.memberID));
                var role = request.Data.GetOrDefault<CorporationRole>(k.role);

                character.IsInTraining().ThrowIfTrue(ErrorCodes.TrainingCharacterInvolved);


                var corporation = character.GetCorporation().CheckAccessAndThrowIfFailed(character, CorporationRole.CEO, CorporationRole.DeputyCEO);

                var isIssuerCEO = corporation.HasAllRoles(character, CorporationRole.CEO);

                //check target
                corporation.IsMember(member).ThrowIfFalse(ErrorCodes.NotMemberOfCorporation);

                _corporationManager.IsInLeavePeriod(member).ThrowIfTrue(ErrorCodes.RoleChangeOnLeavingMember);

                var CEOBitSet = ((int)role & (int)CorporationRole.CEO) > 0;

                if (!isIssuerCEO)
                {
                    //only CEO can set a roles of a CEO
                    corporation.HasAllRoles(member, CorporationRole.CEO).ThrowIfTrue(ErrorCodes.InsufficientPrivileges);
                }

                (CEOBitSet && !isIssuerCEO).ThrowIfTrue(ErrorCodes.InsufficientPrivileges);

                //issuer is ceo and he/she wants to clear his/her own CEO role
                if (isIssuerCEO && (member == character))
                {
                    CEOBitSet.ThrowIfFalse(ErrorCodes.CharacterIsCEOOperationFailed);
                }

                //if the CEO wants to resign
                if (isIssuerCEO && (member != character))
                {
                    //ceo bit set
                    if (CEOBitSet)
                    {
                        //target member VS current corpmember amount check 
                        corporation.CheckMaxMemberCountAndThrowIfFailed(member);

                        //resign as CEO -----------

                        //old roles
                        var currentRole = corporation.GetMemberRole(character);
                        currentRole = currentRole.ClearRole(CorporationRole.CEO);
                        currentRole = currentRole.SetRole(CorporationRole.DeputyCEO);

                        //clear the current CEO's CEO role
                        corporation.SetMemberRole(character, currentRole);

                        //new roles
                        var newRole = corporation.GetMemberRole(character);

                        corporation.WriteRoleHistory(character, character, newRole, currentRole);
                        //%%% remove alliance board nem kene?
                    }
                }

                role = role.CleanUpHangarAccess();
                role = role.CleanUpCharacterPBSRoles();

                //target member's role
                var oldRole = corporation.GetMemberRole(member);

                const int deputyMask = (int)CorporationRole.DeputyCEO;

                var targetDeputyStatus = (int)oldRole & deputyMask;
                var targetNewDeputyStatus = (int)role & deputyMask;

                (targetNewDeputyStatus != targetDeputyStatus && !isIssuerCEO).ThrowIfTrue(ErrorCodes.InsufficientPrivileges);

                //set the actual target member's role
                corporation.SetMemberRole(member, role);
                corporation.WriteRoleHistory(character, member, role, oldRole);
                //            corporation.GetLogger().SetMemberRole(character,member);
                corporation.Save();

                _channelManager.SetMemberRole(corporation.ChannelName, member, role);

                if (member.IsDocked)
                {
                    var dockingBase = _dockingBaseHelper.GetDockingBase(member.CurrentDockingBaseEid);
                    if (dockingBase is PBSDockingBase pbsDockingBase)
                    {
                        _channelManager.SetMemberRole(pbsDockingBase.ChannelName, member, role);
                    }
                }

                CorporationData.RemoveFromCache(corporation.Eid);
                
                scope.Complete();
            }
        }
    }
}
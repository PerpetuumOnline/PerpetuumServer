using System.Linq;
using System.Transactions;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;
using Perpetuum.Units.DockingBases;
using Perpetuum.Zones.ProximityProbes;

namespace Perpetuum.RequestHandlers
{
    public class ProximityProbeRegisterSet : IRequestHandler
    {
        private readonly UnitHelper _unitHelper;

        public ProximityProbeRegisterSet(UnitHelper unitHelper)
        {
            _unitHelper = unitHelper;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;

                var playersToRegister = request.Data.GetOrDefault<int[]>(k.members).Select(Character.Get).ToArray();
                var probeEid = request.Data.GetOrDefault<long>(k.eid);

                playersToRegister.Length.ThrowIfEqual(0, ErrorCodes.WTFErrorMedicalAttentionSuggested);

                var probe = _unitHelper.GetUnitOrThrow<ProximityProbeBase>(probeEid);
                probe.HasAccess(character).ThrowIfError();

                var corporation = character.GetPrivateCorporationOrThrow();

                var members = corporation.GetCharacterMembers();

                var processedCharacters = playersToRegister.Intersect(members).ToArray();

                if (processedCharacters.Length <= 0) 
                    return;

                var bosses = corporation.Members.Where(m => m.role.IsAnyRole(CorporationRole.CEO, CorporationRole.DeputyCEO)).Select(m => m.character).ToArray();
            
                probe.GetMaxRegisteredCount().ThrowIfLess(processedCharacters.Length - bosses.Length,ErrorCodes.MaximumAllowedRegistrationExceeded);
                
                PBSRegisterHelper.ClearMembersFromSql(probeEid);

                //add bosses
                processedCharacters = processedCharacters.Concat(bosses).Distinct().ToArray();

                PBSRegisterHelper.WriteRegistersToDb(probeEid, processedCharacters);

                
                Transaction.Current.OnCommited(() =>
                {
                    probe.ReloadRegistration();
                    probe.SendUpdateToAllPossibleMembers();
                });
                
                scope.Complete();
            }
        }
    }
}
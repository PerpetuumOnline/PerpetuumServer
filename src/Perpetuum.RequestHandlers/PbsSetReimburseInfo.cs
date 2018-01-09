using System.Transactions;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers
{
    public class PBSSetReimburseInfo : PbsReimburseRequestHander
    {
        public override void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var baseEid = request.Data.GetOrDefault<long>(k.baseEID);
                var forCorporation = request.Data.GetOrDefault<int>(k.forCorporation).ToBool();

                var character = request.Session.Character;
                long? corporationEid = null;

                if (forCorporation)
                {
                    var corporation = character.GetPrivateCorporationOrThrow();
                    corporation.IsAnyRole(character, CorporationRole.CEO, CorporationRole.DeputyCEO).ThrowIfFalse(ErrorCodes.InsufficientPrivileges);

                    character = Character.Get(request.Data.GetOrDefault<int>(k.characterID));

                    character.CorporationEid.ThrowIfNotEqual(corporation.Eid, ErrorCodes.NotMemberOfCorporation);
                    corporation.IsAnyRole(character, CorporationRole.CEO, CorporationRole.DeputyCEO).ThrowIfFalse(ErrorCodes.InsufficientPrivileges);

                    Db.Query().CommandText("delete pbsreimburse where corporationeid = @corporationeid").SetParameter("@corporationeid", corporation.Eid).ExecuteNonQuery();
                    corporationEid = corporation.Eid;
                }
                else
                {
                    Db.Query().CommandText("delete pbsreimburse where characterid = @characterid and corporationeid is null").SetParameter("@characterid", character.Id).ExecuteNonQuery();
                }

                Db.Query().CommandText("insert into pbsreimburse (characterid,corporationeid,baseeid) values (@characterid,@corporationeid,@baseeid)")
                    .SetParameter("@characterid", character.Id)
                    .SetParameter("@corporationeid", corporationEid)
                    .SetParameter("@baseeid", baseEid)
                    .ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLInsertError);

                Transaction.Current.OnCommited(() => SendReimburseInfo(request, forCorporation));
                scope.Complete();
            }
        }
    }
}
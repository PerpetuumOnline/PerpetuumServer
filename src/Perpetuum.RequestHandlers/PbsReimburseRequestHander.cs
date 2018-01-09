using System.Collections.Generic;
using System.Data;
using Perpetuum.Data;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers
{
    public abstract class PbsReimburseRequestHander : IRequestHandler
    {
        public abstract void HandleRequest(IRequest request);

        protected static void SendReimburseInfo(IRequest request, bool forCorporation)
        {
            IDataRecord record;

            var character = request.Session.Character;
            if (forCorporation)
            {
                var corporation = character.GetPrivateCorporationOrThrow();
                corporation.IsAnyRole(character, CorporationRole.CEO, CorporationRole.DeputyCEO).ThrowIfFalse(ErrorCodes.InsufficientPrivileges);

                record = Db.Query().CommandText("select * from pbsreimburse where corporationEid = @corporationEid")
                    .SetParameter("@corporationEid", corporation.Eid)
                    .ExecuteSingleRow();
            }
            else
            {
                record = Db.Query().CommandText("select * from pbsreimburse where characterid = @characterId and corporationeid is null")
                    .SetParameter("@characterId", character.Id)
                    .ExecuteSingleRow();
            }

            var result = new Dictionary<string, object>
            {
                {k.forCorporation, forCorporation}
            };

            if (record != null)
            {
                result.Add(k.characterID, record.GetValue<int>("characterid"));
                result.Add(k.corporationEID, record.GetValue<long?>("corporationEid"));
                result.Add(k.baseEID, record.GetValue<long>("baseeid"));
            }

            Message.Builder.FromRequest(request).WithData(result).Send();
        }

    }
}
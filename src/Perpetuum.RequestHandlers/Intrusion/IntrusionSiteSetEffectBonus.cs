using System.Collections.Generic;
using System.Transactions;
using Perpetuum.Data;
using Perpetuum.ExportedTypes;
using Perpetuum.Host.Requests;
using Perpetuum.Zones.Intrusion;

namespace Perpetuum.RequestHandlers.Intrusion
{
    public class IntrusionSiteSetEffectBonus : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var siteEid = request.Data.GetOrDefault<long>(k.target);
                var effectType = (EffectType)request.Data.GetOrDefault<int>(k.effectType);

                var character = request.Session.Character;
                var outpost = request.Zone.GetUnit(siteEid).ThrowIfNotType<Outpost>(ErrorCodes.IntrusionSiteNotFound);
                outpost.SetEffectBonus(effectType, character);

                Transaction.Current.OnCommited(() =>
                {
                    var result = new Dictionary<string, object>
                    {
                        {k.siteEID, siteEid}, 
                        {k.info, outpost.GetIntrusionSiteInfo().ToDictionary()}
                    };

                    Message.Builder.FromRequest(request).WithData(result).Send();
                });
                
                scope.Complete();
            }
        }
    }
}

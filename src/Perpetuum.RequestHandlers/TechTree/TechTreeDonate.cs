using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using Perpetuum.Data;
using Perpetuum.ExportedTypes;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;
using Perpetuum.Services.TechTree;

namespace Perpetuum.RequestHandlers.TechTree
{
    public class TechTreeDonate : TechTreeRequestHandler
    {
        private readonly ITechTreeService _techTreeService;

        public TechTreeDonate(ITechTreeService techTreeService)
        {
            _techTreeService = techTreeService;
        }

        public override void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var points = request.Data.GetOrDefault<IDictionary<string, object>>(k.points).Select(kvp => new Points((TechTreePointType)int.Parse(kvp.Key), (int)kvp.Value)).ToArray();

                var character = request.Session.Character;
                var characterPoints = new TechTreePointsHandler(character.Eid);
                var corporationEid = character.CorporationEid;
                var corporationPoints = new TechTreePointsHandler(corporationEid);

                foreach (var point in points)
                {
                    characterPoints.UpdatePoints(point.type, current =>
                    {
                        current.ThrowIfLess(point.amount, ErrorCodes.TechTreeNotEnoughPoints, gex => gex.SetData("pointType", (int)point.type).SetData("points", point.amount));
                        corporationPoints.UpdatePoints(point.type, c => c + point.amount);

                        var logEvent = new LogEvent(LogType.Donate, character)
                        {
                            CorporationEid = corporationEid,
                            Points = point
                        };

                        TechTreeLogger.WriteLog(logEvent);
                        return current - point.amount;
                    });
                }

                var result = new Dictionary<string, object>();
                characterPoints.AddAvailablePointsToDictionary(result, "characterPoints");

                if (Corporation.GetRoleFromSql(character).HasRole(PresetCorporationRoles.CAN_LIST_TECHTREE))
                {
                    corporationPoints.AddAvailablePointsToDictionary(result, "corporationPoints");
                }

                Message.Builder.FromRequest(request).WithData(result).Send();

                Transaction.Current.OnCommited(() => SendInfoToCorporation(_techTreeService,corporationEid));
                
                scope.Complete();
            }
        }
    }
}
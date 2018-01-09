using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using Perpetuum.Data;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;
using Perpetuum.Services.TechTree;

namespace Perpetuum.RequestHandlers.TechTree
{
    public class TechTreeUnlock : TechTreeRequestHandler
    {
        private readonly ITechTreeService _techTreeService;
        private readonly ITechTreeInfoService _infoService;

        public TechTreeUnlock(ITechTreeService techTreeService,ITechTreeInfoService infoService)
        {
            _techTreeService = techTreeService;
            _infoService = infoService;
        }

        public override void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var definition = request.Data.GetOrDefault<int>(k.definition);
                var forCorporation = request.Data.GetOrDefault<int>(k.forCorporation) == 1;

                var character = request.Session.Character;
                var ownerEid = character.Eid;
                var logEvent = new LogEvent(LogType.Unlock, character) { Definition = definition };

                if (forCorporation)
                {
                    Corporation.GetRoleFromSql(character).HasRole(PresetCorporationRoles.CAN_UNLOCK_TECHTREE).ThrowIfFalse(ErrorCodes.TechTreeAccessDenied);
                    var corporationEid = character.CorporationEid;
                    logEvent.CorporationEid = corporationEid;
                    ownerEid = corporationEid;

                    Transaction.Current.OnCommited(() => SendInfoToCorporation(_techTreeService,corporationEid));
                }

                var points = new TechTreePointsHandler(ownerEid);
                var techTreeNodes = _infoService.GetNodes();
                var node = techTreeNodes.GetOrDefault(definition);
                if (node == null)
                    throw PerpetuumException.Create(ErrorCodes.TechTreeNodeNotFound).SetData("definition", definition);

                var unlockedNodes = _techTreeService.GetUnlockedNodes(ownerEid).ToArray();

                // tudja-e mar
                var any = unlockedNodes.Any(n => n == node);
                if (any)
                    throw PerpetuumException.Create(ErrorCodes.TechTreeAlreadyUnlocked).SetData("definition", definition);

                // parent megvan-e
                var allUnlocked = node.Traverse(techTreeNodes).All(unlockedNodes.Contains);
                if (!allUnlocked)
                    throw new PerpetuumException(ErrorCodes.TechTreeUnlockParentMissing);

                var enablerExtension = node.GetEnablerExtension(_infoService.GetGroupInfos());
                character.CheckLearnedExtension(enablerExtension).ThrowIfFalse(ErrorCodes.TechTreeEnablerExtensionMissing);

                foreach (var price in node.Prices)
                {
                    var amount = forCorporation ? price.amount * _infoService.CorporationPriceMultiplier : price.amount;

                    points.UpdatePoints(price.type, current =>
                    {
                        if (current < amount)
                            throw PerpetuumException.Create(ErrorCodes.TechTreeNotEnoughPoints).SetData("pointType", (int)price.type).SetData("points", amount);

                        logEvent.Points = price;
                        TechTreeLogger.WriteLog(logEvent);
                        return current - amount;
                    });
                }

                var r = Db.Query().CommandText("insert into techtreeunlockednodes (owner,definition) values (@owner,@definition)")
                    .SetParameter("@owner", ownerEid)
                    .SetParameter("@definition", node.Definition)
                    .ExecuteNonQuery();

                if (r == 0)
                    throw new PerpetuumException(ErrorCodes.SQLInsertError);

                Transaction.Current.OnCommited(() => _techTreeService.NodeUnlocked(ownerEid, node));

                var result = new Dictionary<string, object>
                {
                    {k.definition, definition},
                    {k.forCorporation, forCorporation}
                };

                points.AddAvailablePointsToDictionary(result);
                Message.Builder.FromRequest(request).WithData(result).Send();
                
                scope.Complete();
            }
        }
    }
}
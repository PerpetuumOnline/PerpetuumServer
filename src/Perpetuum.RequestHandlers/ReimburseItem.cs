using Perpetuum.Accounting;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Items.Templates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace Perpetuum.RequestHandlers
{
    public class ReimburseItemRequestHandler : IRequestHandler
    {
        private readonly IRobotTemplateReader _robotTemplateReader;
        private readonly IAccountManager _accountManager;
        private readonly CreateItemRequestHandler _createItemRequestHandler;

        public ReimburseItemRequestHandler(IRobotTemplateReader robotTemplateReader, IAccountManager accountManager)
        {
            _robotTemplateReader = robotTemplateReader;
            _accountManager = accountManager;
            _createItemRequestHandler = new CreateItemRequestHandler(_robotTemplateReader);
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = Character.Get(request.Data.GetOrDefault<int>(k.characterID));
                var targetContainer = character.GetPublicContainerWithItems();
                var item = _createItemRequestHandler.CreateItem(request).ThrowIfNull(ErrorCodes.WTFErrorMedicalAttentionSuggested);
                item.Owner = character.Eid;

                string type = (item is Robots.Robot) ? "Robot" : "Item";

                LogReimbursement(request, type, item.ED.Definition, item.Quantity);

                targetContainer.AddItem(item, false);
                targetContainer.Save();

                Message.Builder.FromRequest(request)
                    .WithData(item.ToDictionary())
                    .WrapToResult()
                    .Send();

                Transaction.Current.OnCommited(() => character.ReloadContainerOnZoneAsync());

                scope.Complete();
            }
        }

        private void LogReimbursement(IRequest request, string ItemType, int itemid, int qty)
        {

            Db.Query().CommandText("INSERT INTO opp_reimburselog (ReimburseTo, ReimburseBy, ReimburseTime, EntityId, ItemType, Qty) VALUES (@ReimburseTo, @ReimburseBy, @ReimburseTime, @EntityId, @ItemType, @Qty)")
                    .SetParameter("@ReimburseTo", request.Data.GetOrDefault<int>(k.characterID))
                    .SetParameter("@ReimburseBy", request.Session.Character.Id)
                    .SetParameter("@ReimburseTime", DateTime.Now)
                    .SetParameter("@EntityId", itemid)
                    .SetParameter("@ItemType", ItemType)
                    .SetParameter("@Qty", qty)
                    .ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLInsertError);
        }
    }
}
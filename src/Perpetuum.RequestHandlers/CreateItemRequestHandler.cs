using System.Transactions;
using Perpetuum.Containers;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.Host.Requests;
using Perpetuum.Items;
using Perpetuum.Items.Templates;

namespace Perpetuum.RequestHandlers
{
    public class CreateItemRequestHandler : IRequestHandler
    {
        private readonly IRobotTemplateReader _robotTemplateReader;

        public CreateItemRequestHandler(IRobotTemplateReader robotTemplateReader)
        {
            _robotTemplateReader = robotTemplateReader;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var targetContainer = GetTargetContainer(request);
                var item = CreateItem(request).ThrowIfNull(ErrorCodes.WTFErrorMedicalAttentionSuggested);
                item.Owner = character.Eid;

                targetContainer.AddItem(item,false);
                targetContainer.Save();

                Message.Builder.FromRequest(request)
                    .WithData(item.ToDictionary())
                    .WrapToResult()
                    .Send();

                Transaction.Current.OnCommited(() => character.ReloadContainerOnZoneAsync());
                
                scope.Complete();
            }
        }

        private static Container GetTargetContainer(IRequest request)
        {
            var containerEid = request.Data.GetOrDefault(k.targetContainer, 0L);

            var character = request.Session.Character;
            var container = containerEid != 0L ? Container.GetWithItems(containerEid, character) : character.GetPublicContainerWithItems();

            return container;
        }

        [CanBeNull]
        private Item CreateItem(IRequest request)
        {
            var definition = request.Data.GetOrDefault<int>(k.definition);
            if (definition > 0)
            {
                var item = (Item)Entity.Factory.CreateWithRandomEID(definition);
                item.Quantity = request.Data.GetOrDefault(k.quantity, 1);
                return item;
            }

            var robotTemplateId = request.Data.GetOrDefault<int>(k.templateID);
            if (robotTemplateId <= 0)
                return null;

            var template = _robotTemplateReader.Get(robotTemplateId);
            var robot = template?.Build();
            if (robot == null)
                return null;

            var character = request.Session.Character;
            robot.Initialize(character);
            robot.Repair();
            return robot;
        }
    }
}
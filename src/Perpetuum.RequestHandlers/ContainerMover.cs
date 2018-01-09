using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Containers;
using Perpetuum.Data;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers
{
    public class ContainerMover : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;

                var containerName = request.Data.GetOrDefault<string>(k.name);
                var targetCharacter = Character.Get(request.Data.GetOrDefault<int>(k.characterID));
                var targetContainerEid = request.Data.GetOrDefault<long>(k.container);

                var publicContainer = character.GetPublicContainerWithItems();

                var sourceContainer = publicContainer.GetItems(true).OfType<Container>().FirstOrDefault(container => ContainerFinder(containerName, container)).ThrowIfNull(ErrorCodes.ContainerNotFound);

                var b = TransactionLogEvent.Builder().SetTransactionType(TransactionType.AddContainerContent).SetCharacter(character);
                var items = sourceContainer.GetItems().ToArray();
                foreach (var item in items)
                {
                    b.SetItem(item);
                    character.LogTransaction(b);
                }

                var targetContainer = Container.GetOrThrow(targetContainerEid);
                sourceContainer.RelocateItems(character, targetCharacter, items, targetContainer);
                sourceContainer.Save();
                targetContainer.Save();

                Message.Builder.FromRequest(request).WithOk().Send();
                
                scope.Complete();
            }
        }

        private static bool ContainerFinder(string containerName, Container container)
        {
            var itemName = container?.Name;
            if (string.IsNullOrEmpty(itemName))
                return false;

            return itemName.StartsWith(containerName);
        }
    }
}
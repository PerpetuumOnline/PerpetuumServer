using Perpetuum.Containers;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Host.Requests;
using Perpetuum.Items;
using Perpetuum.Robots;

namespace Perpetuum.RequestHandlers
{
    public class StackTo : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var targetEid = request.Data.GetOrDefault<long>(k.eid);
                var includeRobotCargos = request.Data.GetOrDefault<int>(k.inventory) == 1;
                var containerEid = request.Data.GetOrDefault<long>(k.container);

                character.IsDocked.ThrowIfFalse(ErrorCodes.CharacterHasToBeDocked);

                var container = Container.GetWithItems(containerEid, character, ContainerAccess.Remove);

                var targetItem = container.GetItemOrThrow(targetEid, true);
                targetItem.ED.AttributeFlags.AlwaysStackable.ThrowIfFalse(ErrorCodes.OnlyAlwaysStackableIsSupported);
                var parentContainerOfTarget = targetItem.GetOrLoadParentEntity().ThrowIfNotType<Container>(ErrorCodes.WTFErrorMedicalAttentionSuggested);
                parentContainerOfTarget.CheckAccessAndThrowIfFailed(character, ContainerAccess.Delete);

                //only from container and infinite box
                parentContainerOfTarget.IsCategory(CategoryFlags.cf_infinite_capacity_containers).ThrowIfFalse(ErrorCodes.ContainerHasToBeInfinite);

                var quantitySum = 0;
                using (var notifier = new ItemErrorNotifier(true))
                {
                    foreach (var item in container.GetItems(true))
                    {
                        try
                        {
                            if (item.Definition != targetItem.Definition || item.Eid == targetItem.Eid)
                                continue;

                            //get the parent of the current item
                            var currentParent = item.GetOrLoadParentEntity() as Container;
                            if (currentParent == null)
                                continue;

                            if (!includeRobotCargos)
                            {
                                if (currentParent is RobotInventory)
                                {
                                    //skip items in any robotcargo
                                    continue;
                                }
                            }

                            if (currentParent is VolumeWrapperContainer)
                            {
                                currentParent.CheckAccessAndThrowIfFailed(character, ContainerAccess.Delete);
                            }

                            quantitySum += item.Quantity;
                            Entity.Repository.Delete(item);
                        }
                        catch (PerpetuumException gex)
                        {
                            notifier.AddError(item, gex);
                        }
                    }
                }

                targetItem.Quantity += quantitySum;
                container.Save();

                Message.Builder.SetCommand(Commands.ListContainer).WithData(container.ToDictionary()).ToClient(request.Session).Send();
                
                scope.Complete();
            }
        }
    }
}
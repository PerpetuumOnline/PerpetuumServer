using System.Collections.Generic;
using Perpetuum.Containers;
using Perpetuum.Data;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;
using Perpetuum.Robots;

namespace Perpetuum.RequestHandlers
{
    public class RelocateItems : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var itemEids = request.Data.GetOrDefault<long[]>(k.eid);
                var sourceContainerEid = request.Data.GetOrDefault<long>(k.sourceContainer);
                var targetContainerEid = request.Data.GetOrDefault<long>(k.targetContainer);

                if (sourceContainerEid == targetContainerEid)
                    return;

                Container.GetContainersWithItems(character, sourceContainerEid, targetContainerEid, out Container sourceContainer, out Container targetContainer);

                if (targetContainer is RobotInventory)
                {
                    CorporateHangar.Contains(targetContainer).ThrowIfTrue(ErrorCodes.AccessDenied);
                }

                sourceContainer.RelocateItems(character, character, itemEids, targetContainer);
                sourceContainer.Save();
                targetContainer.Save();

                var targetList = targetContainer.ToDictionary();

                try
                {
                    targetContainer.CheckAccessAndThrowIfFailed(character, ContainerAccess.List);
                }
                catch (PerpetuumException gex)
                {
                    if (gex.error == ErrorCodes.InsufficientPrivileges)
                    {
                        targetList[k.items] = new Dictionary<string, object>();
                    }
                    else
                        throw;
                }

                var result = new Dictionary<string, object>
                {
                    {k.source, sourceContainer.ToDictionary()},
                    {k.target, targetList}
                };

                Message.Builder.FromRequest(request).WithData(result).Send();
                
                scope.Complete();
            }
        }
    }
}
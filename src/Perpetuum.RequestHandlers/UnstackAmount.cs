using System.Collections.Generic;
using Perpetuum.Containers;
using Perpetuum.Data;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;
using Perpetuum.Robots;

namespace Perpetuum.RequestHandlers
{
    public class UnstackAmount : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var itemEid = request.Data.GetOrDefault<long>(k.eid);
                var amount = request.Data.GetOrDefault<int>(k.amount);
                var size = request.Data.GetOrDefault<int>(k.size);
                var sourceContainerEid = request.Data.GetOrDefault<long>(k.container);
                var targetContainerEid = request.Data.GetOrDefault<long>(k.targetContainer);

                size.ThrowIfLessOrEqual(0, ErrorCodes.WTFErrorMedicalAttentionSuggested);
                amount.ThrowIfLessOrEqual(0, ErrorCodes.WTFErrorMedicalAttentionSuggested);

                Db.Query().CommandText("select eid from entities (UPDLOCK) where eid = @eid").SetParameter("@eid", itemEid).ExecuteNonQuery();

                //load the container
                Container.GetContainersWithItems(character, sourceContainerEid, targetContainerEid, out Container sourceContainer, out Container targetContainer);

                if (targetContainer is RobotInventory)
                {
                    CorporateHangar.Contains(targetContainer).ThrowIfTrue(ErrorCodes.AccessDenied);
                }

                var result = new Dictionary<string, object>();

                if (sourceContainerEid == targetContainerEid)
                {
                    sourceContainer.UnstackItem(itemEid, character, amount, size, sourceContainer);
                }
                else
                {
                    sourceContainer.UnstackItem(itemEid, character, amount, size, targetContainer);

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
                        {
                            throw;
                        }
                    }

                    result.Add(k.target, targetList);
                }

                sourceContainer.Save();

                result.Add(k.source, sourceContainer.ToDictionary());
                Message.Builder.FromRequest(request).WithData(result).Send();
                
                scope.Complete();
            }
        }
    }
}
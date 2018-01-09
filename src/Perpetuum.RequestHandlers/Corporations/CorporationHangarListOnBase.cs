using System.Linq;
using Perpetuum.Containers;
using Perpetuum.Host.Requests;
using Perpetuum.Items;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationHangarListOnBase : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;
            var hangarEid = request.Data.GetOrDefault<long>(k.eid);

            character.IsDocked.ThrowIfFalse(ErrorCodes.CharacterHasToBeDocked);

            var mainHangar = Container.GetOrThrow(hangarEid);
            mainHangar.Parent.ThrowIfNotEqual(character.CurrentDockingBaseEid, ErrorCodes.FacilityOutOfReach);

            var corporationEid = character.CorporationEid;
            mainHangar.ReloadItems(corporationEid);
            mainHangar.Initialize(character);
            mainHangar.CheckAccessAndThrowIfFailed(character, ContainerAccess.List);

            var result = mainHangar.BaseInfoToDictionary();
            result[k.items] = mainHangar.GetItems().Where(y => y.Owner == corporationEid).ToDictionary("c", h =>
                {
                    var hangarInfo = h.ToDictionary();
                    hangarInfo.Remove(k.items);
                    hangarInfo[k.noItemsSent] = 1;
                    return hangarInfo;
                });

            Message.Builder.FromRequest(request).WithData(result).WrapToResult().Send();
        }
    }
}
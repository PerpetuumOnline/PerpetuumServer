using Perpetuum.Containers;
using Perpetuum.Data;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationHangarLogClear : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var hangarEid = request.Data.GetOrDefault<long>(k.eid);

                var corporateHangar = character.GetCorporation().GetHangar(hangarEid, character, ContainerAccess.LogClear);
                corporateHangar.ContainerLogger.ClearLog(character);
                corporateHangar.Save();

                Message.Builder.SetCommand(Commands.CorporationHangarLogList)
                    .WithData(corporateHangar.ContainerLogger.LogsToDictionary())
                    .ToCharacter(character)
                    .Send();

                Message.Builder.FromRequest(request).WithOk().Send();
                
                scope.Complete();
            }
        }
    }
}
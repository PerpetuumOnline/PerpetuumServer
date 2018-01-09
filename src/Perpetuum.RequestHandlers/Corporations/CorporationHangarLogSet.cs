using Perpetuum.Containers;
using Perpetuum.Data;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationHangarLogSet : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var log = request.Data.GetOrDefault<int>(k.log) == 1;
                var hangarEid = request.Data.GetOrDefault<long>(k.eid);

                //load the hangar with different checks
                var corporateHangar = character.GetCorporation().GetHangar(hangarEid, character, ContainerAccess.LogStart);
                corporateHangar.ReloadItems(corporateHangar.Owner);
                corporateHangar.SetLogging(log, character, true);
                corporateHangar.Save();

                Message.Builder.FromRequest(request)
                    .WithData(corporateHangar.ToDictionary())
                    .Send();

                Message.Builder.SetCommand(Commands.CorporationHangarLogList)
                    .WithData(corporateHangar.ContainerLogger.LogsToDictionary())
                    .ToCharacter(character)
                    .Send();
                
                scope.Complete();
            }
        }
    }
}
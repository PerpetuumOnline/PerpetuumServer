using Perpetuum.Containers;
using Perpetuum.Data;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationHangarSetName : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var name = request.Data.GetOrDefault<string>(k.name);
                var hangarEid = request.Data.GetOrDefault<long>(k.eid);

                var corporation = character.GetCorporation();
                var corporateHangar = corporation.GetHangar(hangarEid, character, ContainerAccess.LogList);
                corporateHangar.Name = name;
                corporateHangar.Save();

                Message.Builder.SetCommand(request.Command)
                    .WithData(corporateHangar.ToDictionary())
                    .ToCharacters(corporation.GetCharacterMembers())
                    .Send();
                
                scope.Complete();
            }
        }
    }
}
using Perpetuum.Host.Requests;
using Perpetuum.Zones.Scanning.Results;

namespace Perpetuum.RequestHandlers
{
    public class MineralScanResultList : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;
            var repo = new MineralScanResultRepository(character);
            var result = repo.GetAll().ToDictionary("s",r => r.ToDictionary());
            Message.Builder.FromRequest(request).WithData(result).Send();
        }
    }
}

using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationLogHistory : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;
            var corporation = character.GetPrivateCorporationOrThrow();
            var logger = corporation.GetLogger();

            var offset = request.Data.GetOrDefault(k.offset, 1);
            var result = logger.GetHistory(offset);
            Message.Builder.FromRequest(request).WithData(result).Send();
        }
    }
}
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Extensions
{
    public class ExtensionLearntList : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var result = request.Session.Character.GetExtensions().ToDictionary("e", e => e.ToDictionary());
            Message.Builder.FromRequest(request).WithData(result).WrapToResult().Send();
        }
    }
}
using System.Collections.Generic;
using Perpetuum.Host.Requests;
using Perpetuum.Services.ExtensionService;

namespace Perpetuum.RequestHandlers.Extensions
{
    public class ExtensionGetPointParameters : IRequestHandler
    {
        private readonly ExtensionPoints _extensionPoints;

        public ExtensionGetPointParameters(ExtensionPoints extensionPoints)
        {
            _extensionPoints = extensionPoints;
        }

        public void HandleRequest(IRequest request)
        {
            var result = _extensionPoints.points.ToDictionary("a", e => new Dictionary<string, object>
            {
                {k.points, e.Value},
                {k.rank, e.Key}
            });

            Message.Builder.FromRequest(request).WithData(result).Send();
        }
    }
}
using System.Collections.Generic;
using Perpetuum.Host.Requests;
using Perpetuum.Services.ExtensionService;

namespace Perpetuum.RequestHandlers.Extensions
{
    public class ExtensionGetAll : IRequestHandler
    {
        private readonly Dictionary<string, object> _extensionInfos;

        public ExtensionGetAll(IExtensionReader extensionReader)
        {
            _extensionInfos = extensionReader.GetExtensions().ToDictionary("e", ex => ex.Value.ToDictionary());
        }

        public void HandleRequest(IRequest request)
        {
            Message.Builder.FromRequest(request).WithData(_extensionInfos).Send();
        }
    }
}
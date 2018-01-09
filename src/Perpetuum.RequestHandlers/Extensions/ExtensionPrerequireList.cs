using System.Collections.Generic;
using System.Linq;
using Perpetuum.Host.Requests;
using Perpetuum.Services.ExtensionService;

namespace Perpetuum.RequestHandlers.Extensions
{
    public class ExtensionPrerequireList : IRequestHandler
    {
        private readonly Dictionary<string, object> _requiredExtensionInfos;

        public ExtensionPrerequireList(IExtensionReader extensionReader)
        {
            _requiredExtensionInfos = extensionReader.GetExtensions().Values.SelectMany(info => info.RequiredExtensions, (info, reqExtension) =>
            {
                return new Dictionary<string, object>
                {
                    {k.extensionID, info.id},
                    {k.requiredExtension, reqExtension.id},
                    {k.requiredLevel, reqExtension.level}
                };
            }).ToDictionary("e", e => e);
        }

        public void HandleRequest(IRequest request)
        {
            Message.Builder.FromRequest(request).WithData(_requiredExtensionInfos).Send();
        }
    }
}
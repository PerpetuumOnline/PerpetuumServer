using System.Collections.Generic;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationDocumentConfig : IRequestHandler
    {
        private readonly Dictionary<string, object> _configInfos;

        public CorporationDocumentConfig()
        {
            _configInfos = CorporationDocumentHelper.corporationDocumentConfig.Values.ToDictionary("c", c => c.ToDictionary());
        }

        public void HandleRequest(IRequest request)
        {
            Message.Builder.FromRequest(request).WithData(_configInfos).WithEmpty().Send();
        }
    }
}

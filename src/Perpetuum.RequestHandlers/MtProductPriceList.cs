using System.Collections.Generic;
using Perpetuum.Accounting;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers
{
    public class MtProductPriceList : IRequestHandler
    {
        private readonly MtProductHelper _mtProductHelper;

        public MtProductPriceList(MtProductHelper mtProductHelper)
        {
            _mtProductHelper = mtProductHelper;
        }

        public void HandleRequest(IRequest request)
        {
            var data = new Dictionary<string, object>
            {
                {"products", _mtProductHelper.GetProductInfos()}
            };

            Message.Builder.FromRequest(request)
                .WithData(data)
                .Send();
        }
    }
}
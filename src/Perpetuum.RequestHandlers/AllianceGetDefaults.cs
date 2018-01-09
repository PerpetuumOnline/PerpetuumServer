using System.Collections.Generic;
using Perpetuum.Groups.Alliances;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers
{
    public class AllianceGetDefaults : IRequestHandler
    {
        private readonly Dictionary<string, object> _allianceInfos;

        public AllianceGetDefaults()
        {
            _allianceInfos = AllianceHelper.GetAllianceInfo();
        }

        public void HandleRequest(IRequest request)
        {
            Message.Builder.FromRequest(request).WithData(_allianceInfos).Send();
        }
    }
}
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers
{
    public class PBSGetReimburseInfo : PbsReimburseRequestHander
    {
        public override void HandleRequest(IRequest request)
        {
            var forCorporation = request.Data.GetOrDefault<int>(k.forCorporation).ToBool();
            SendReimburseInfo(request, forCorporation);
        }
    }
}
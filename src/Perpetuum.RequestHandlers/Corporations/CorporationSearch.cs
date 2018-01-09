using System.Linq;
using Perpetuum.Data;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationSearch : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var pattern = request.Data.GetOrDefault<string>(k.name);

            if (pattern.Equals(string.Empty) || pattern.Length < 2)
            {
                throw new PerpetuumException(ErrorCodes.SearchStringTooShort);
            }

            pattern = $"%{pattern}%";

            var corporationEids = Db.Query().CommandText("select top (32) eid from corporations where ([name] like @pattern or nick like @pattern) and defaultcorp=0")
                .SetParameter("@pattern", pattern)
                .Execute()
                .Select(r => DataRecordExtensions.GetValue<long>(r, 0))
                .ToArray();

            if (corporationEids.Any())
            {
                var result = CorporationData.GetAnyInfoDictionary(corporationEids);
                Message.Builder.FromRequest(request).WithData(result).WrapToResult().Send();
            }
            else
            {
                Message.Builder.FromRequest(request).WithEmpty().Send();
            }
        }
    }
}
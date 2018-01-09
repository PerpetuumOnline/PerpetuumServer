using System.Collections.Generic;
using System.Linq;
using Perpetuum.Data;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers
{
    public class GetDefinitionConfigUnits : IRequestHandler
    {
        private readonly Dictionary<string, object> _configCache;

        public GetDefinitionConfigUnits()
        {
            _configCache = new Dictionary<string, object>
            {
                {"definitionConfigUnits", GetDefinitionConfigDict()}
            };
        }

        private static Dictionary<string, object> GetDefinitionConfigDict()
        {
            return Db.Query().CommandText("select * from definitionconfigunits")
                .Execute()
                .ToDictionary(r => r.GetValue<string>("configname"),
                    r =>
                    {
                        return (object)new Dictionary<string, object>
                        {
                            {k.measurementOffset, r.GetValue<double>("measurementoffset")},
                            {k.digits, r.GetValue<int>("digits")},
                            {k.measurementMultiplier, r.GetValue<double>("measurementmultiplier")},
                        };
                    });
        }

        public void HandleRequest(IRequest request)
        {
            Message.Builder.FromRequest(request).WithData(_configCache).Send();
        }
    }
}
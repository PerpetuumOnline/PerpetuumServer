using System.Collections.Generic;
using Perpetuum.Data;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers
{
    public class GetAggregateFields : IRequestHandler
    {
        private readonly Dictionary<string, object> _aggregateFieldInfos;

        public GetAggregateFields()
        {
            _aggregateFieldInfos = LoadAggregateInfos();
        }

        private static Dictionary<string, object> LoadAggregateInfos()
        {
            return Db.Query().CommandText("select * from aggregatefields").Execute()
                           .ToDictionary("a", record => new Dictionary<string, object>
                           {
                               {k.ID, DataRecordExtensions.GetValue<int>(record, "id")},
                               {k.name, DataRecordExtensions.GetValue<string>(record, "name")},
                               {k.formula, DataRecordExtensions.GetValue<int>(record, "formula")},
                               {k.measurementUnit, DataRecordExtensions.GetValue<string>(record, "measurementunit")},
                               {k.measurementMultiplier, DataRecordExtensions.GetValue<double>(record, "measurementmultiplier")},
                               {k.measurementOffset, DataRecordExtensions.GetValue<double>(record, "measurementoffset")},
                               {k.category, DataRecordExtensions.GetValue<int>(record, "category")},
                               {k.digits, DataRecordExtensions.GetValue<int>(record, "digits")}
                           });
        }

        public void HandleRequest(IRequest request)
        {
            Message.Builder.FromRequest(request)
                           .WithData(_aggregateFieldInfos)
                           .Send();
        }
    }
}
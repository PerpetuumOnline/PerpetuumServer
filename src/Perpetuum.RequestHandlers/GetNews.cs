using System;
using System.Collections.Generic;
using Perpetuum.Data;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers
{
    public class GetNews : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var newsAmount = request.Data.GetOrDefault<int>(k.amount);
            var newsLanguage = request.Data.GetOrDefault<int>(k.language);

            var result = Db.Query().CommandText("select top(@amount) title,body,ntime,type,idx,language from news where language=@language or language=0 order by idx desc")
                .SetParameter("@amount", newsAmount)
                .SetParameter("@language", newsLanguage)
                .Execute()
                .ToDictionary("news_", record => new Dictionary<string, object>(6)
                {
                    {k.title, record.GetValue<string>(0)},
                    {k.body, record.GetValue<string>(1)},
                    {k.time, record.GetValue<DateTime>(2)},
                    {k.type, record.GetValue<int>(3)},
                    {k.index, record.GetValue<int>(4)},
                    {k.language, record.GetValue<int>(5)}
                });

            Message.Builder.FromRequest(request).WithData(result).WrapToResult().WithEmpty().Send();
        }
    }
}
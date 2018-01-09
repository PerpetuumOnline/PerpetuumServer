using System.Collections.Generic;
using Perpetuum.Data;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers
{
    public class NewsCategory : IRequestHandler
    {
        private readonly Dictionary<string, object> _newsCategoryInfos;

        public NewsCategory()
        {
            _newsCategoryInfos = Db.Query().CommandText("select id,category from newscategories").Execute()
                .ToDictionary("n", r => new Dictionary<string, object>
                {
                    {k.ID, DataRecordExtensions.GetValue<int>(r, 0)},
                    {k.category, DataRecordExtensions.GetValue<string>(r, 1)}
                });
        }

        public void HandleRequest(IRequest request)
        {
            Message.Builder.FromRequest(request).WithData(_newsCategoryInfos).WrapToResult().WithEmpty().Send();
        }
    }
}
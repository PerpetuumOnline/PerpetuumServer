using System.Collections.Generic;
using Perpetuum.Data;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers
{
    public class DecorCategoryList : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var decorCategoryList = Db.Query().CommandText("select id,categoryname from decorcategories").Execute()
                .ToDictionary("d", r => new Dictionary<string, object>
                {
                    {k.ID, DataRecordExtensions.GetValue<int>(r, 0)},
                    {k.name, DataRecordExtensions.GetValue<string>(r, 1)}
                });

            var result = new Dictionary<string, object> { { k.data, decorCategoryList } };
            Message.Builder.FromRequest(request).WithData(result).Send();
        }
    }
}
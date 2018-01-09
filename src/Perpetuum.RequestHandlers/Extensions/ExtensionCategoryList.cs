using System.Collections.Generic;
using Perpetuum.Data;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Extensions
{
    public class ExtensionCategoryList : IRequestHandler
    {
        private readonly Dictionary<string, object> _extensionCategoryInfos;

        public ExtensionCategoryList()
        {
            _extensionCategoryInfos = LoadExtensionCategories();
        }

        private static Dictionary<string, object> LoadExtensionCategories()
        {
            var result = Db.Query().CommandText("select extensioncategoryid,categoryname,hidden from extensioncategories").Execute()
                .ToDictionary("c", r => new Dictionary<string, object>
                {
                    {k.ID, DataRecordExtensions.GetValue<int>(r, 0)},
                    {k.name, DataRecordExtensions.GetValue<string>(r, 1)},
                    {k.hidden, DataRecordExtensions.GetValue<bool>(r, 2)},
                });

            return result;
        }

        public void HandleRequest(IRequest request)
        {
            Message.Builder.FromRequest(request).WithData(_extensionCategoryInfos).Send();
        }
    }
}
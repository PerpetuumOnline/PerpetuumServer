using System.Collections.Generic;
using Perpetuum.ExportedTypes;

namespace Perpetuum.Services.TechTree
{
    public class TechTreeGroupInfo
    {
        public TechTreeGroup group;
        public int enablerExtensionId;
        public int displayOrder;

        public IDictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                {k.group,(int)group},
                {k.extensionID,enablerExtensionId},
                {k.displayOrder,displayOrder}
            };
        }
    }
}
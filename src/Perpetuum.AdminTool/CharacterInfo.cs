using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace Perpetuum.AdminTool
{
    public class CharacterInfo : DynamicObject
    {
        private readonly Dictionary<string, object> _info;

        public CharacterInfo(Dictionary<string,object> info)
        {
            _info = info.ToDictionary(kvp => kvp.Key.ToLower(), kvp => kvp.Value);
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            return _info.TryGetValue(binder.Name.ToLower(), out result);
        }
    }
}
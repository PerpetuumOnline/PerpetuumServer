using System.Dynamic;

namespace Perpetuum.AdminTool
{
    public class Locator : DynamicObject
    {
        private readonly Resolver _resolver;

        public delegate object Resolver(string name);

        public Locator(Resolver resolver)
        {
            _resolver = resolver;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = _resolver(binder.Name);
            return result != null;
        }
    }
}
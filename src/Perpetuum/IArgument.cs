using System.Collections.Generic;

namespace Perpetuum
{
    public interface IArgument
    {
        void Check(Dictionary<string, object> data);
    }
}
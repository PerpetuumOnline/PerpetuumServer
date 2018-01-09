using System.Collections.Generic;

namespace Perpetuum
{
    public interface IMessage
    {
        Command Command { get; }
        IDictionary<string,object> Data { get; }
        byte[] ToBytes();
    }
}
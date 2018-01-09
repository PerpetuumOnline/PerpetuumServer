using Perpetuum.Items;

namespace Perpetuum.Common
{
    public static class BinaryStreamExtensions
    {
        public static void AppendProperty(this BinaryStream stream,ItemProperty property)
        {
            stream.AppendInt((int)property.Field);
            stream.AppendDouble(property.Value);
        }
    }
}

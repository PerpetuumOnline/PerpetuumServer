using System;
using System.Collections.Generic;
using System.Linq;

namespace Perpetuum.Units
{
    public static class OptionalPropertyExtensions
    {
        public static void WriteToStream(this IEnumerable<IOptionalProperty> properties, BinaryStream stream)
        {
            var p = properties ?? new IOptionalProperty[0];

            stream.AppendInt(p.Count());

            foreach (var property in p)
            {
                stream.AppendByte((byte) property.Type);
                stream.AppendObject(property.Value);
            }
        }

        public static bool TryGetOptionalPropertiesForLooting(this IEnumerable<IOptionalProperty> properties, out Guid missionGuid, out int displayOrder)
        {
            missionGuid = Guid.Empty;
            displayOrder = -1;

            if (properties == null)
                return false;

            var result = false;
            foreach (var property in properties)
            {
                if (property.Type == UnitDataType.MissionDisplayOrder)
                {
                    displayOrder = (int)property.Value;
                    result = true;
                }

                if (property.Type == UnitDataType.MissionGuid)
                {
                    missionGuid = (Guid)property.Value;
                    result = true;
                }
            }

            return result;
        }
    }
}
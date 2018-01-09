using System;

namespace Perpetuum.Zones.Terrains.Materials
{
    public static class MaterialTypeExtensions
    {
        public static string GetName(this MaterialType materialType)
        {
            var name = Enum.GetName(typeof(MaterialType), materialType) ?? string.Empty;
            return name.ToLower();
        }

        public static MaterialType ToMaterialType(this string materialName)
        {
            return !Enum.TryParse(materialName, true, out MaterialType result) ? MaterialType.Undefined : result;
        }
    }
}

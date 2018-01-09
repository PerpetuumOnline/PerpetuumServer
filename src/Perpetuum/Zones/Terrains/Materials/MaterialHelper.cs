using System.Collections.Generic;
using System.Linq;
using Perpetuum.Data;
using Perpetuum.EntityFramework;

namespace Perpetuum.Zones.Terrains.Materials
{
    public class MaterialHelper
    {
        private readonly Dictionary<MaterialType, MaterialInfo> _materials;

        public MaterialHelper(IEntityDefaultReader entityDefaultReader)
        {
            _materials = LoadMaterials(entityDefaultReader);
        }

        private static Dictionary<MaterialType, MaterialInfo> LoadMaterials(IEntityDefaultReader entityDefaultReader)
        {
            return Db.Query().CommandText("select * from minerals").Execute().Select(r =>
            {
                var mineral = new MaterialInfo
                {
                    Type = r.GetValue<string>("name").ToMaterialType(),
                    EntityDefault = entityDefaultReader.Get(r.GetValue<int>("definition")),
                    Amount = r.GetValue<int>("amount"),
                    EnablerExtensionRequired = r.GetValue<bool>("enablereffectrequired")
                };
                return mineral;
            }).ToDictionary(m => m.Type);
        }

        public MaterialInfo GetMaterialInfo(MaterialType type)
        {
            return _materials.GetOrDefault(type,MaterialInfo.None);
        }
    }
}
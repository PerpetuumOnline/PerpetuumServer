using System.Collections.Generic;
using System.Linq;
using Perpetuum.Data;
using Perpetuum.GenXY;

namespace Perpetuum.Zones.Environments
{
    public static class EntityEnvironment
    {
        public static EntityEnvironmentDescription LoadEnvironmentSql(int definition)
        {
            var descriptionString = Db.Query()
                .CommandText("select descriptionstring from environmentdescription where definition=@definition")
                .SetParameter("@definition", definition)
                .ExecuteScalar<string>();

            if (descriptionString == null)
            {
                return new EntityEnvironmentDescription();
            }

            return ConvertFromString(descriptionString);
        }

        public static EntityEnvironmentDescription LoadEnvironmentFromStagingSql(int definition)
        {
            var descriptionString = Db.Query()
                .CommandText("select descriptionstring from environmentdescriptionstaging where definition=@definition")
                .SetParameter("@definition", definition)
                .ExecuteScalar<string>();

            if (descriptionString == null)
            {
                return new EntityEnvironmentDescription();
            }

            return ConvertFromString(descriptionString);
        }

        private static EntityEnvironmentDescription ConvertFromString(string descriptionString )
        {
            var nativeDescription = new EntityEnvironmentDescription();

            var descriptionObjects = GenxyConverter.Deserialize(descriptionString);
            
            if (descriptionObjects.ContainsKey(k.blocks))
            {
                var tileDict = (Dictionary<string, object>)descriptionObjects[k.blocks];
                nativeDescription.blocksTiles = ConvertTilesToList(tileDict);
            }

            return nativeDescription;
        }

        private static List<Tile> ConvertTilesToList(Dictionary<string, object> tileDict)
        {
            return tileDict.Values.Select(t => Tile.FromDictionary((Dictionary<string, object>) t)).ToList();
        }

        public static void WriteEnvironmentToSql(int definition, EntityEnvironmentDescription nativeEnvironmentData)
        {
            var descriptionString = GenxyConverter.Serialize(nativeEnvironmentData.ToDictionary());
            Db.Query().CommandText("writeEnvironment")
                .SetParameter("@definition", definition)
                .SetParameter("@descriptionstring", descriptionString)
                .ExecuteScalar<int>().ThrowIfEqual(0,ErrorCodes.SQLInsertError);
        }
       
    }
}
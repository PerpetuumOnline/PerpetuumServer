using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Perpetuum.Data;
using Perpetuum.GenXY;
using Perpetuum.Host.Requests;
using Perpetuum.Services.ExtensionService;

namespace Perpetuum.RequestHandlers.Characters
{
    public class CharacterWizardData : IRequestHandler
    {
        private readonly Dictionary<string,object> _characterWizardData;

        public CharacterWizardData(IExtensionReader extensionReader)
        {
            _characterWizardData = CreateCharacterWizardData(extensionReader);
        }

        private static Dictionary<string,object> CreateCharacterWizardData(IExtensionReader extensionReader)
        {
            var extensions = extensionReader.GetExtensions();
            var corpProfiles = Db.Query().CommandText("select eid,publicprofile from corporations where defaultcorp = 1")
                .Execute()
                .ToDictionary(r => r.GetValue("eid"),r => GenxyConverter.Deserialize(r.GetValue<string>("publicprofile")));

            Dictionary<string,object> LoadCwData(string name,string idName,Action<IDataRecord,Dictionary<string,object>> action = null)
            {
                var cw = Db.Query().CommandText($"select * from {name}").Execute();
                var ex = Db.Query().CommandText($"select * from {name}_extension")
                    .Execute()
                    .Where(r => extensions.ContainsKey(r.GetValue<int>("extensionid"))).ToLookup(r => r.GetValue(idName));

                return cw.ToDictionary("w",r =>
                {
                    var id = r.GetValue(idName);
                    var dd = new Dictionary<string,object>
                    {
                        ["ID"] = id,
                        ["name"] = r.GetValue("name"),
                        ["description"] = r.GetValue("descriptiontoken"),
                        ["extension"] = ex.GetOrEmpty(id).ToDictionary("e",x =>
                        {
                            return new Dictionary<string,object>
                            {
                                {k.extensionID, x.GetValue("extensionid")},
                                {k.add, x.GetValue("levelincrement")}
                            };
                        })
                    };

                    action?.Invoke(r,dd);
                    return dd;
                });
            }

            var result = new Dictionary<string,object>
            {
                ["race"] = LoadCwData("cw_race","raceid"),
                ["spark"] = LoadCwData("cw_spark","sparkid"),
                ["school"] = LoadCwData("cw_school","schoolid",(r,d) => { d["raceID"] = r.GetValue("raceid"); }),
                ["major"] = LoadCwData("cw_major","majorid",(r,d) => { d["schoolID"] = r.GetValue("schoolid"); }),
                ["corporation"] = LoadCwData("cw_corporation","corporationEID",(r,d) =>
                {
                    d["baseEID"] = r.GetValue("baseEID");
                    d["schoolID"] = r.GetValue("schoolid");
                    d["publicProfile"] = corpProfiles[r.GetValue<long>("corporationEID")];
                })
            };

            return result;
        }

        public void HandleRequest(IRequest request)
        {
            Message.Builder.FromRequest(request).WrapToResult().WithData(_characterWizardData).Send();
        }
    }
}
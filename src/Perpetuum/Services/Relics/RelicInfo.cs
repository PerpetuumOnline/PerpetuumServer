using Perpetuum.Data;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Perpetuum.Services.Relics
{
    public class RelicInfo
    {
        public static RelicInfo CreateRelicInfoFromRecord(IDataRecord record)
        {
            var id = record.GetValue<int>("id");
            var name = record.GetValue<string>("name");
            var raceid = record.GetValue<int?>("raceid");
            var level = record.GetValue<int?>("level");
            var ep = record.GetValue<int?>("ep");
            var info = new RelicInfo(id, name, raceid, level, ep);

            return info;
        }

        public static RelicInfo GetByIDFromDB(int id)
        {
            var relicinfos = Db.Query().CommandText("SELECT TOP 1 id, name, raceid, level, ep FROM relictypes WHERE id = @relicInfoId")
                .SetParameter("@relicInfoId", id)
                .Execute()
                .Select(CreateRelicInfoFromRecord);

            return relicinfos.SingleOrDefault();
        }

        public static RelicInfo GetByNameFromDB(string name)
        {
            var relicinfos = Db.Query().CommandText("SELECT TOP 1 id, name, raceid, level, ep FROM relictypes WHERE name = @name")
                .SetParameter("@name", name)
                .Execute()
                .Select(CreateRelicInfoFromRecord);

            return relicinfos.SingleOrDefault();
        }

        private int _id;
        private string _name;
        private int? _raceid;
        private int? _level;
        private int? _ep;
        private Position _staticRelicPosistion;
        public bool HasStaticPosistion = false;

        public RelicInfo(int id, string name, int? raceid, int? level, int? ep)
        {
            _id = id;
            _name = name;
            _ep = ep;
            _raceid = raceid;
            _level = level;
        }

        public int GetLevel()
        {
            return this._level ?? 0;
        }

        public int GetFaction()
        {
            return this._raceid ?? 0;
        }

        public void SetPosition(Position p)
        {
            HasStaticPosistion = true;
            _staticRelicPosistion = p;
        }

        public Position GetPosition()
        {
            return _staticRelicPosistion;
        }

        public int GetEP()
        {
            return this._ep ?? 5;
        }

        public int GetID()
        {
            return this._id;
        }

        public Dictionary<string, object> ToDictionary()
        {
            var dictionary = new Dictionary<string, object>
            {
                {k.name, this._name },
                {k.raceID, this.GetFaction()},
                {k.level, this.GetLevel()},
                {k.extensionPoints, this.GetEP()},
                {"isStatic", HasStaticPosistion},
            };

            return dictionary;
        }

    }
}

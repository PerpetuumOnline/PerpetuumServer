using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Converters;
using Perpetuum.Data;

namespace Perpetuum.Groups.Corporations.Applications
{
    public class CorporationApplication : IConvertible<IDictionary<string, object>>
    {
        private DateTime _applyTime;
        public long CorporationEID { get; private set; }
        public Character Character { get; set; }
        public string Motivation { private get; set; }

        private CorporationApplication() { }

        public CorporationApplication(PrivateCorporation corporation)
        {
            CorporationEID = corporation.Eid;
        }

        public Dictionary<string, object > ToDictionary()
        {
            return new Dictionary<string, object>
            {
                {k.characterID, Character.Id},
                {k.corporationEID, CorporationEID},
                {k.date, _applyTime},
                {k.note, Motivation }
            };
        }

        private static readonly IConverter<IDataRecord,CorporationApplication> _converter = new Converter();

        private class Converter : IConverter<IDataRecord,CorporationApplication>
        {
            public CorporationApplication Convert(IDataRecord record)
            {
                return new CorporationApplication
                {
                    CorporationEID = record.GetValue<long>("corporationEID"),
                    Character = Character.Get(record.GetValue<int>("characterID")),
                    _applyTime = record.GetValue<DateTime>("applyTime"),
                    Motivation = record.GetValue<string>("motivation")
                };
            }
        }

        public void InsertToDb()
        {
            Db.Query().CommandText("insert corporationApplication (characterID, corporationEID, motivation) values (@characterID, @corporationEID, @motivation)")
                .SetParameter("@characterID", Character.Id)
                .SetParameter("@corporationEID", CorporationEID)
                .SetParameter("@motivation", Motivation)
                .ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLInsertError);
        }

        public void DeleteFromDb()
        {
            Db.Query().CommandText("delete from corporationApplication where characterID=@characterID and corporationEID = @corporationEid")
                    .SetParameter("@characterID", Character.Id)
                    .SetParameter("@corporationEid",CorporationEID)
                    .ExecuteNonQuery();
        }

        public static IEnumerable<CorporationApplication> GetAllByCorporation(PrivateCorporation corporation)
        {
            return Db.Query().CommandText("select * from corporationApplication where corporationEID=@corporationEID")
                           .SetParameter("@corporationEID", corporation.Eid)
                           .Execute()
                           .ConvertAll(_converter)
                           .ToArray();
        }

        public static IEnumerable<CorporationApplication> GetAllByCharacter(Character character)
        {
            return Db.Query().CommandText("select * from corporationApplication where characterID=@characterID")
                           .SetParameter("@characterID", character.Id)
                           .Execute()
                           .ConvertAll(_converter)
                           .ToArray();
        }

        public IDictionary<string, object> ConvertTo()
        {
            return ToDictionary();
        }
    }
}
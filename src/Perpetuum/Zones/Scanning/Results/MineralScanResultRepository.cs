using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.Zones.Terrains.Materials;

namespace Perpetuum.Zones.Scanning.Results
{
    public class MineralScanResultRepository
    {
        private readonly Character _owner;

        public delegate MineralScanResultRepository Factory(Character owner);

        public MineralScanResultRepository(Character owner)
        {
            _owner = owner;
        }

        public void InsertOrThrow(MineralScanResult result)
        {
            var id = Db.Query().CommandText(@"insert into mineralscan (ownerid,creation,zoneid,materialtype,x1,y1,x2,y2,scanAccuracy) 
                                                        values (@ownerid,@creation,@zoneid,@materialtype,@x1,@y1,@x2,@y2,@scanAccuracy);select cast(scope_identity() as int)")
                .SetParameter("@ownerId", _owner.Id)
                .SetParameter("@creation", DateTime.Now)
                .SetParameter("@zoneid",result.ZoneId)
                .SetParameter("@materialType", (byte)result.MaterialType)
                .SetParameter("@x1", result.Area.X1)
                .SetParameter("@y1", result.Area.Y1)
                .SetParameter("@x2", result.Area.X2)
                .SetParameter("@y2", result.Area.Y2)
                .SetParameter("@scanAccuracy",result.ScanAccuracy)
                .SetParameter("@quality",result.Quality)
                .ExecuteScalar<int>().ThrowIfEqual(0, ErrorCodes.SQLInsertError);

            result.Id = id;
        }

        public void DeleteById(int id)
        {
            Db.Query().CommandText("delete mineralscan where ownerid=@characterID and id = @id")
                .SetParameter("@id",id)
                .SetParameter("@characterID", _owner.Id)
                .ExecuteNonQuery();
        }

        public void DeleteAll()
        {
            Db.Query().CommandText("delete mineralscan where ownerid=@characterID")
                .SetParameter("@characterID",_owner.Id)
                .ExecuteNonQuery();
        }

        public void MoveToFolderById(int id, string folder)
        {
            Db.Query().CommandText("update mineralscan set folder = @folder where ownerid = @ownerId and id = @id")
                .SetParameter("@id",id)
                .SetParameter("@ownerId",_owner.Id)
                .SetParameter("@folder", folder)
                .ExecuteNonQuery().ThrowIfZero(ErrorCodes.SQLUpdateError);
        }

        [CanBeNull]
        public MineralScanResult Get(int id)
        {
            var record = Db.Query().CommandText("select * from mineralscan where id = @id and ownerid = @ownerId")
                .SetParameter("@id", id)
                .SetParameter("@ownerId", _owner.Id)
                .ExecuteSingleRow();

            if (record == null)
                return null;

            return CreateFromRecord(record);
        }

        public List<MineralScanResult> GetAll()
        {
            return Db.Query().CommandText("select * from mineralscan where ownerid = @ownerId")
                .SetParameter("@ownerId",_owner.Id)
                .Execute()
                .Select(CreateFromRecord)
                .ToList();
        }

        private static MineralScanResult CreateFromRecord(IDataRecord record)
        {
            var result = new MineralScanResult();

            result.Id = record.GetValue<int>("id");
            result.Folder = record.GetValue<string>("folder");
            result.FoundAny = true;
            result.MaterialType = (MaterialType)Convert.ToInt32(record.GetValue("materialType"));
            result.ZoneId = record.GetValue<int>("zoneId");
            result.Creation = record.GetValue<DateTime>("creation");
            result.Quality = record.GetValue<long>("quality");

            var x1 = record.GetValue<int>("x1");
            var y1 = record.GetValue<int>("y1");
            var x2 = record.GetValue<int>("x2");
            var y2 = record.GetValue<int>("y2");

            result.Area = new Area(x1,y1,x2,y2);
            result.ScanAccuracy = record.GetValue<double>("scanaccuracy");

            return result;
        }
    }
}
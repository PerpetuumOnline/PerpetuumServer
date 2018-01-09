using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.Units.DockingBases;

namespace Perpetuum.Services.Sparks.Teleports
{
    public class SparkTeleportRepository : ISparkTeleportRepository
    {
        private readonly DockingBaseHelper _dockingBaseHelper;

        public SparkTeleportRepository(DockingBaseHelper dockingBaseHelper)
        {
            _dockingBaseHelper = dockingBaseHelper;
        }

        public SparkTeleport Get(int id)
        {
            var record = Db.Query().CommandText("select * from charactersparkteleports where id=@ID and characterid=@characterID")
                .SetParameter("@ID", id)
                .ExecuteSingleRow();

            if (record == null)
                return null;

            return CreateFromRecord(record);
        }

        public IEnumerable<SparkTeleport> GetAll()
        {
            throw new NotImplementedException();
        }

        public void Insert(SparkTeleport sparkTeleport)
        {
            sparkTeleport.ID = Db.Query().CommandText("insert charactersparkteleports (characterid,baseeid,basedefinition,zoneid,x,y) values (@characterID, @baseEID, @baseDefinition, @zoneID, @x, @y); select cast(scope_identity() as integer")
                .SetParameter("@characterID",sparkTeleport.Character.Id)
                .SetParameter("@baseEID",sparkTeleport.DockingBase.Eid)
                .SetParameter("@baseDefinition", sparkTeleport.DockingBase.Definition)
                .SetParameter("@zoneId", sparkTeleport.DockingBase.Zone.Id)
                .SetParameter("@x", sparkTeleport.DockingBase.CurrentPosition.intX)
                .SetParameter("@y", sparkTeleport.DockingBase.CurrentPosition.intY)
                .ExecuteNonQuery();
            
            if (sparkTeleport.ID == 0)
                throw new PerpetuumException(ErrorCodes.SQLInsertError);
        }

        public void Update(SparkTeleport item)
        {
            throw new NotImplementedException();
        }


        public void Delete(SparkTeleport sparkTeleport)
        {
            var res = Db.Query().CommandText("delete charactersparkteleports where id=@ID")
                .SetParameter("@ID", sparkTeleport.ID)
                .ExecuteNonQuery();
            
            if (res == 0)
                throw new PerpetuumException(ErrorCodes.SQLDeleteError);
        }


        public IEnumerable<SparkTeleport> GetAllByCharacter(Character character)
        {
            return Db.Query().CommandText("select * from charactersparkteleports where characterid=@characterID")
                .SetParameter("@characterID", character.Id)
                .Execute()
                .Select(CreateFromRecord);
        }

        public IEnumerable<SparkTeleport> GetAllByDockingBase(DockingBase dockingBase)
        {
            return Db.Query().CommandText("select * from charactersparkteleports where baseeid=@baseEid")
                .SetParameter("@baseEid", dockingBase.Eid)
                .Execute()
                .Select(CreateFromRecord);
        }

        private SparkTeleport CreateFromRecord(IDataRecord record)
        {
            var teleport = new SparkTeleport();
            teleport.ID = record.GetValue<int>("id");
            var baseEid = record.GetValue<long>("baseeid");
            teleport.DockingBase = _dockingBaseHelper.GetDockingBase(baseEid);
            teleport.Character = Character.Get(record.GetValue<int>("characterid"));
            return teleport;
        }
    }
}
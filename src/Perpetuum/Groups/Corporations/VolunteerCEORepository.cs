using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.EntityFramework;

namespace Perpetuum.Groups.Corporations
{
    public class VolunteerCEORepository : IVolunteerCEORepository
    {
        private readonly IEntityRepository _entityRepository;

        public VolunteerCEORepository(IEntityRepository entityRepository)
        {
            _entityRepository = entityRepository;
        }

        public VolunteerCEO Get(long id)
        {
            return GetAll().FirstOrDefault(v => v.corporation.Eid == id);
        }

        public IEnumerable<VolunteerCEO> GetAll()
        {
            return Db.Query().CommandText("select * from corporationceotakeover").Execute().Select(CreateVolunteerCEOFromRecord).ToArray();
        }

        public void Insert(VolunteerCEO volunteerCEO)
        {
            var res = Db.Query().CommandText("insert corporationceotakeover (corporationeid,characterid,expiry) values (@corporationEID, @characterID,@expiry)")
                .SetParameter("@characterID",volunteerCEO.character.Id)
                .SetParameter("@corporationEID",volunteerCEO.corporation.Eid)
                .SetParameter("@expiry",volunteerCEO.expiry)
                .ExecuteNonQuery();

            if(res == 0 )
                throw new PerpetuumException(ErrorCodes.SQLInsertError);
        }

        public void Update(VolunteerCEO item)
        {
            throw new NotImplementedException();
        }

        public void Delete(VolunteerCEO volunteerCEO)
        {
            var res = Db.Query().CommandText("delete corporationceotakeover where corporationeid=@corporationEID")
                .SetParameter("@corporationEID",volunteerCEO.corporation.Eid)
                .ExecuteNonQuery();
            if (res == 0)
                throw new PerpetuumException(ErrorCodes.SQLDeleteError);
        }

        private VolunteerCEO CreateVolunteerCEOFromRecord(IDataRecord record)
        {
            var corporationEid = record.GetValue<long>("corporationeid");
            var v = new VolunteerCEO
            {
                character = Character.Get(record.GetValue<int>("characterid")),
                expiry = record.GetValue<DateTime>("expiry"),
                corporation =  (Corporation) _entityRepository.Load(corporationEid)
            };
            return v;
        }
    }
}
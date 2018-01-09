using System;
using System.Collections.Generic;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;

namespace Perpetuum.Groups.Gangs
{
    public class GangRepository : IGangRepository
    {
        private readonly Gang.Factory _gangFactory;

        public GangRepository(Gang.Factory gangFactory)
        {
            _gangFactory = gangFactory;
        }

        public void Insert(Gang gang)
        {
            var res = Db.Query().CommandText("insert into gang (id,leaderid,name) values (@id,@leaderId,@name)")
                .SetParameter("@id",gang.Id)
                .SetParameter("@leaderId",gang.Leader.Id)
                .SetParameter("@name", gang.Name)
                .ExecuteNonQuery();

            if (res == 0)
                throw new PerpetuumException(ErrorCodes.SQLInsertError);

            foreach (var member in gang.GetMembers())
            {
                InsertMember(gang,member);
            }
        }

        public void InsertMember(Gang gang, Character member)
        {
            var res = Db.Query().CommandText("insert into gangmembers (gangid,memberid) values (@gangid,@memberId)")
                .SetParameter("@gangid",gang.Id)
                .SetParameter("@memberId", member.Id).ExecuteNonQuery();

            if (res == 0)
                throw new PerpetuumException(ErrorCodes.SQLInsertError);
        }

        public void DeleteMember(Gang gang, Character member)
        {
            var res = Db.Query().CommandText("delete from gangmembers where gangid = @gangId and memberid = @memberId")
                .SetParameter("@gangId", gang.Id)
                .SetParameter("@memberId", member.Id).ExecuteNonQuery();

            if (res == 0)
                throw new PerpetuumException(ErrorCodes.SQLDeleteError);
        }

        public Guid GetGangIDByMember(Character member)
        {
            var gangID = Db.Query().CommandText("select gangid from gangmembers where memberid = @memberId")
                .SetParameter("@memberId", member.Id).ExecuteScalar<Guid>();

            return gangID;
        }

        public void Delete(Gang gang)
        {
            Db.Query().CommandText("delete from gangmembers where gangid = @gangGuid;delete from gang where id = @gangGuid")
                .SetParameter("@gangGuid",gang.Id).ExecuteNonQuery();

        }

        public void UpdateLeader(Gang gang, Character newLeader)
        {
            var res = Db.Query().CommandText("update gang set leaderid = @leaderid where id = @gangGuid")
                .SetParameter("@gangGuid",gang.Id)
                .SetParameter("@leaderId", newLeader.Id).ExecuteNonQuery();

            if (res == 0)
                throw new PerpetuumException(ErrorCodes.SQLUpdateError);
        }

        public Gang Get(Guid gangID)
        {
            var gangRecord = Db.Query().CommandText("select leaderid,name from gang where id = @gangGuid")
                .SetParameter("@gangGuid", gangID)
                .ExecuteSingleRow();

            if (gangRecord == null)
                return null;

            var gang = _gangFactory();
            gang.Id = gangID;
            gang.Leader = Character.Get(gangRecord.GetValue<int>(0));
            gang.Name = gangRecord.GetValue<string>(1);

            var records = Db.Query().CommandText("select memberid,role from gangmembers where gangid = @gangGuid")
                .SetParameter("@gangGuid",gangID).Execute();

            foreach (var record in records)
            {
                var member = Character.Get(record.GetValue<int>(0));
                var role = record.GetValue<GangRole>(1);

                gang.SetMember(member,role);
            }

            return gang;
        }

        public void UpdateMemberRole(Gang gang, Character member, GangRole newRole)
        {
            var res = Db.Query().CommandText("update gangmembers set role = @newRole where gangid = @gangId and memberid = @memberId")
                .SetParameter("@gangId",gang.Id)
                .SetParameter("@memberId", member.Id)
                .SetParameter("@newRole", (int)newRole)
                .ExecuteNonQuery();

            if (res == 0)
                throw new PerpetuumException(ErrorCodes.SQLUpdateError);
        }

        public IEnumerable<Gang> GetAll()
        {
            throw new NotImplementedException();
        }

        public void Update(Gang item)
        {
            throw new NotImplementedException();
        }
    }
}
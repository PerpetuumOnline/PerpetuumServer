using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;

namespace Perpetuum.Groups.Corporations
{
    /// <summary>
    /// Represents the root of a bulletin
    /// </summary>
    public struct BulletinDescription
    {
        public static readonly BulletinDescription None = new BulletinDescription();

        public int bulletinID;
        public long groupEID;
        public string title;
        public DateTime startDate;
        public int startedBy;

        public Dictionary<string, object> ToDictionary()
        {
            var dictionary = new Dictionary<string, object>
            {
                [k.bulletinID] = bulletinID,
                [k.groupEID] = groupEID,
                [k.title] = title,
                [k.started] = startDate,
                [k.startedBy] =  startedBy
            };
            return dictionary;
        }
    }

    public interface IBulletinHandler
    {
        BulletinDescription GetBulletin(int bulletinID);
        BulletinDescription GetBulletin(int bulletinID, long groupEID);
        BulletinDescription StartBulletin(long corporationEid, string title, Character character);
        void DeleteBulletin(int bulletinID);
        bool BulletinExists(int bulletinID, long corporationEid);

        int InsertEntry(int bulletinID, int characterId, string entryText);
        void UpdateEntry(int bulletinID, int entryID, string entryText);
        void DeleteEntry(int bulletinID, int entryID);
        int CountEntries(int bulletinID);
        int GetEntryOwner(int bulletinID, int entryID);

        Dictionary<string, object> GetBulletinEntries(int bulletinBulletinID);
        Dictionary<string, object> GetNewBulletinEntries(DateTime startTime, long corporationEid);
        Dictionary<string, object> GetBulletinList(long corporationEid);

        void SendBulletinUpdate(BulletinDescription bulletin, CorporationBulletinEvent bulletinStarted, Character character);
    }

    /// <summary>
    /// This class handles all bulletin activity
    /// </summary>
    public class BulletinHandler : IBulletinHandler
    {
        public BulletinDescription GetBulletin(int bulletinID)
        {
            var record = Db.Query().CommandText("select bulletinID,groupEID,title,startdate,startedby from bulletins where bulletinID=@bulletinID")
                                .SetParameter("@bulletinID", bulletinID)
                                .ExecuteSingleRow();

            if (record == null)
                return BulletinDescription.None;

            var bulletin = CreateBulletinDescriptionFromRecord(record);
            return bulletin;
        }

        public BulletinDescription GetBulletin(int bulletinID, long groupEID)
        {
            var record = Db.Query().CommandText("select bulletinID,groupEID,title,startdate,startedby from bulletins where bulletinID=@bulletinID and groupEID=@groupEID")
                                 .SetParameter("@bulletinID", bulletinID)
                                 .SetParameter("@groupEID", groupEID)
                                 .ExecuteSingleRow();
            var bulletinDescription = CreateBulletinDescriptionFromRecord(record);
            return bulletinDescription;
        }

        private static BulletinDescription CreateBulletinDescriptionFromRecord(IDataRecord record)
        {
            var bulletin = new BulletinDescription
            {
                bulletinID = record.GetValue<int>(0),
                groupEID = record.GetValue<long>(1),
                title = record.GetValue<string>(2),
                startDate = record.GetValue<DateTime>(3),
                startedBy = record.GetValue<int>(4)
            };

            return bulletin;
        }

        public void DeleteBulletin(int bulletinID)
        {
            Db.Query().CommandText("delete bulletinentries where bulletinID=@bulletinID; delete bulletins where bulletinID=@bulletinID")
                   .SetParameter("@bulletinID", bulletinID)
                   .ExecuteNonQuery();
        }


        public Dictionary<string, object> GetBulletinList(long groupEID)
        {
            var records = Db.Query().CommandText("select bulletinID, title from bulletins where groupEID=@groupEID")
                                 .SetParameter("groupEID", groupEID)
                                 .Execute();

            var counter = 0;
            return (from b in records select (object) new Dictionary<string, object>
            {
                {k.bulletinID, b.GetValue<int>(0)},
                { k.title, b.GetValue<string>(1)}
            }).ToDictionary(d => "c" + counter++);
        }


        public Dictionary<string, object> GetBulletinEntries(int bulletinID)
        {
            var counter = 0;
            var entries = new Dictionary<string, object>();
            
            var records = Db.Query().CommandText("select entryID,characterID,entrytext,entrydate from bulletinentries where bulletinID=@bulletinID")
                                 .SetParameter("@bulletinID", bulletinID)
                                 .Execute();

            foreach (var record in records)
           {
               var oneEntry = new Dictionary<string, object>(4)
                                  {
                                      {k.entryID, record.GetValue<int>(0)},
                                      {k.characterID, record.GetValue<int>(1)},
                                      {k.text, record.GetValue<string>(2)},
                                      {k.date, record.GetValue<DateTime>(3)}
                                  };

               entries.Add("b"+counter++, oneEntry);
           }

            return new Dictionary<string, object>(2)
            {
                {k.bulletinID, bulletinID},
                {k.entries, entries}
            };
        }

        public Dictionary<string, object > GetNewBulletinEntries(DateTime startTime, long groupEID)
        {

            var records = Db.Query().CommandText("select entryID,characterID,entrytext,entrydate,e.bulletinid,b.title from bulletinentries e join bulletins b on b.bulletinid=e.bulletinid where entrydate>@startTime and e.bulletinid in (select bulletinid from bulletins where groupEID=@groupEID)")
                                 .SetParameter("@groupEID", groupEID)
                                 .SetParameter("@startTime", startTime)
                                 .Execute();

            var count = 0;
            return (from r in records
                 select (object) new Dictionary<string, object>
                                     {
                                         {k.entryID, r.GetValue<int>(0)},
                                         {k.characterID, r.GetValue<int>(1)},
                                         {k.text, r.GetValue<string>(2)},
                                         {k.date, r.GetValue<DateTime>(3)},
                                         {k.bulletinID, r.GetValue<int>(4)},
                                         {k.title, r.GetValue<string>(5)}
                                     }
                ).ToDictionary(d => "c" + count++);
        }

        public BulletinDescription StartBulletin(long groupEID, string title, Character startedBy)
        {
            var bulletinID = Db.Query().CommandText("insert bulletins (groupEID,title,startedby) values (@groupEID,@title,@startedBy); select cast (scope_identity() as int)")
                            .SetParameter("@groupEID", groupEID)
                            .SetParameter("@title", title)
                            .SetParameter("@startedBy", startedBy.Id)
                            .ExecuteScalar<int>();

            var bd = new BulletinDescription
                         {
                            bulletinID = bulletinID,
                            title= title,
                            groupEID=groupEID,
                            startDate= DateTime.Now,
                            startedBy=startedBy.Id
                         };
            return bd;
        }

        public void UpdateEntry(int bulletinID, int entryID, string entryText)
        {
            var res = Db.Query().CommandText("update bulletinentries set entrytext=@entryText where bulletinID=@bulletinID and entryID=@entryID")
                             .SetParameter("@bulletinID", bulletinID)
                             .SetParameter("@entryID", entryID)
                             .SetParameter("@entryText", entryText)
                             .ExecuteNonQuery();

            if (res == 0)
                throw new PerpetuumException(ErrorCodes.SQLDeleteError);
        }

        public int InsertEntry(int bulletinID, int characterID, string entryText)
        {
            return Db.Query().CommandText("insert bulletinentries (bulletinID,characterID,entrytext) values (@bulletinID,@characterID,@entryText);select cast (scope_identity() as int)")
                          .SetParameter("@bulletinID", bulletinID)
                          .SetParameter("@characterID", characterID)
                          .SetParameter("@entryText", entryText)
                          .ExecuteScalar<int>();
        }

        public void DeleteEntry(int bullentiID, int entryID)
        {
            var res = Db.Query().CommandText("delete bulletinentries where bulletinID=@bulletinID and entryID=@entryID")
                             .SetParameter("@bulletinID", bullentiID)
                             .SetParameter("@entryID", entryID)
                             .ExecuteNonQuery();

            if ( res == 0 )
                throw new PerpetuumException(ErrorCodes.SQLDeleteError);
        }

        public int CountEntries(int bulletinID)
        {
            return Db.Query().CommandText("select count(*) from bulletinentries where bulletinID=@bulletinID")
                          .SetParameter("@bulletinID",bulletinID)
                          .ExecuteScalar<int>();
        }

        public int GetEntryOwner(int bulletinID, int entryID)
        {
            return Db.Query().CommandText("select characterID from bulletinentries where bulletinID=@bulletinID and entryID=@entryID")
                          .SetParameter("@bulletinID", bulletinID)
                          .SetParameter("@entryID", entryID)
                          .ExecuteScalar<int>();
        }

        public bool BulletinExists(int bulletinID, long groupEID)
        {
            var id = Db.Query().CommandText("select bulletinID from bulletins where bulletinID=@bulletinID and groupEID=@groupEID")
                                       .SetParameter("@bulletinID", bulletinID)
                                       .SetParameter("@groupEID", groupEID)
                                       .ExecuteScalar<int>();
            return id > 0;
        }

        public void SendBulletinUpdate(BulletinDescription bulletin, CorporationBulletinEvent bulletinEvent, Character issuer)
        {
            var result = new Dictionary<string, object>
                {
                    {k.description, bulletin.ToDictionary()},
                    {k.eventType, (int) bulletinEvent},
                    {k.characterID, issuer.Id}
                };

            var corporation = Corporation.GetOrThrow(bulletin.groupEID);

            Message.Builder.SetCommand(Commands.CorporationBulletinUpdate)
                           .WithData(result)
                           .ToCorporation(corporation)
                           .Send();
        }
    }
}

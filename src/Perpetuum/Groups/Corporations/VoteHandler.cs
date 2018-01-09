using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;

namespace Perpetuum.Groups.Corporations
{
    public enum VoteType
    {
        Freeform,
        WarDeclaration
    }

    public struct VoteEntry
    {
        public Character member;
        public bool answer;

        public Dictionary<string,object> ToDictionary()
        {
            return new Dictionary<string,object>()
            {
                [k.memberID] = member.Id,
                [k.answer] = answer
            };
        }
    }

    public class Vote
    {
        public int voteID;
        public long groupEID;
        public string voteName;
        public string voteTopic;
        public int participation;
        public VoteType voteType;
        public bool closed;
        public DateTime startDate;
        public DateTime? endDate;
        public bool? result;
        public Character startedBy;
        private int _consensusRate;

        public int ConsensusRate
        {
            get { return _consensusRate; }
            set { _consensusRate = value.Clamp(0, 100); }
        }

        public Vote()
        {
            startDate = DateTime.Now;
        }

        public Dictionary<string, object> ToDictionary()
        {
            var oneVote = new Dictionary<string,object>(11)
                              {
                                  {k.voteID, voteID},
                                  {k.groupEID, groupEID},
                                  {k.name, voteName},
                                  {k.topic, voteTopic},
                                  {k.participation, participation},
                                  {k.type, (int) voteType},
                                  {k.closed, closed},
                                  {k.startTime, startDate},
                                  {k.endTime,endDate },
                                  {k.startedBy, startedBy},
                                  {k.consensusRate, _consensusRate},
                                  {k.result,result}
                              };
            return oneVote;
        }
    }

    public interface IVoteHandler
    {
        int VoteCount(long groupEID);
        void UpdateTopic(Vote vote);
        void CastVote(Vote vote, Character character, bool voteChoice);

        [CanBeNull]
        Vote GetVote(int voteID);
        void InsertVote(Vote vote);
        void DeleteVote(Vote vote);
        void EvaluateVote(Vote vote);

        int[] GetMyOpenVotes(Character character, long groupEID);

        IEnumerable<Vote> GetVotesByGroup(long groupEID);

        IEnumerable<VoteEntry> GetVoteEntries(Vote vote);
    }

    public class VoteHandler : IVoteHandler
    {
        public IEnumerable<Vote> GetVotesByGroup(long groupEID)
        {
            var records = Db.Query().CommandText("select * from votes where groupEID=@groupEID")
                                 .SetParameter("@groupEID", groupEID)
                                 .Execute();

            return records.Select(CreateVoteDescriptionFromRecord);
        }

        public Vote GetVote(int voteID)
        {
            var record = Db.Query().CommandText("select * from votes where voteid=@voteID")
                                .SetParameter("@voteID", voteID)
                                .ExecuteSingleRow();

            if (record == null)
                return null;

            return CreateVoteDescriptionFromRecord(record);
        }

        private static Vote CreateVoteDescriptionFromRecord(IDataRecord record)
        {
            var vd = new Vote
            {
                voteID = record.GetValue<int>("voteid"),
                voteName = record.GetValue<string>("votename"),
                voteTopic = record.GetValue<string>("votetopic"),
                participation = record.GetValue<int>("participation"),
                voteType = record.GetValue<VoteType>("votetype"),
                closed = record.GetValue<bool>("closed"),
                startDate = record.GetValue<DateTime>("startdate"),
                result = record.GetValue<bool?>("result"),
                startedBy = Character.Get(record.GetValue<int>("startedby")),
                groupEID = record.GetValue<long>("groupEID"),
                ConsensusRate = record.GetValue<int>("consensusrate"),
                endDate = record.GetValue<DateTime?>("enddate")
            };

            return vd;
        }

        public void CastVote(Vote vote,Character character, bool voteChoice)
        {
           Db.Query().CommandText("insert voteentries (voteid,characterid,voteentry) values (@voteID,@characterID,@voteEntry)")
               .SetParameter("@voteID", vote.voteID)
               .SetParameter("@characterID", character.Id)
               .SetParameter("@voteEntry", voteChoice)
               .ExecuteNonQuery().ThrowIfEqual(0,ErrorCodes.SQLInsertError);
        }

        public IEnumerable<VoteEntry> GetVoteEntries(Vote vote)
        {
            return Db.Query().CommandText("select * from voteentries where voteID=@voteID")
                          .SetParameter("@voteID", vote.voteID)
                          .Execute()
                          .Select(r =>
                          {
                            return new VoteEntry
                            {
                                member = Character.Get(r.GetValue<int>("characterid")),
                                answer = r.GetValue<bool>("voteentry")
                            };
                          }).ToArray();
        }


        public void EvaluateVote(Vote vote)
        {
            var voteEntries = GetVoteEntries(vote).ToArray();

            if (vote.participation < voteEntries.Length)
                return;

            //ok, participation reached
            var yesCount = voteEntries.Count(e => e.answer);
            var yesRate = (double)yesCount / vote.participation * 100.0;

            var result = yesRate >= vote.ConsensusRate;

            var res = Db.Query().CommandText("update votes set closed=1, result=@result, enddate=@endDate where voteID=@voteID")
                .SetParameter("@result", result)
                .SetParameter("@endDate", DateTime.Now)
                .SetParameter("@voteID", vote.voteID)
                .ExecuteNonQuery();

            if ( res == 0 )
                throw new PerpetuumException(ErrorCodes.SQLUpdateError);
        }

        public void InsertVote(Vote vote)
        {
            const string cmdText = @"insert votes (groupEID,votename,votetopic,participation,votetype,startedby,consensusrate) 
                                           values (@groupEID,@voteName,@voteTopic,@participation,@voteType,@startedBy,@consensusRate);
                                           select cast(scope_identity() as int)";

            vote.voteID = Db.Query().CommandText(cmdText)
                                 .SetParameter("@groupEID",vote.groupEID)
                                 .SetParameter("@voteName",vote.voteName)
                                 .SetParameter("@voteTopic",vote.voteTopic)
                                 .SetParameter("@participation",vote.participation)
                                 .SetParameter("@voteType", (int)vote.voteType)
                                 .SetParameter("@startedBy",vote.startedBy.Id)
                                 .SetParameter("@consensusRate",vote.ConsensusRate)
                                 .ExecuteScalar<int>();
        }

        public void UpdateTopic(Vote vote)
        {
            Db.Query().CommandText("update votes set votetopic=@topic where voteID=@voteID")
                   .SetParameter("@voteID", vote.voteID)
                   .SetParameter("@topic", vote.voteTopic)
                   .ExecuteNonQuery().ThrowIfEqual(0,ErrorCodes.SQLUpdateError);
        }

        public int VoteCount(long groupEID)
        {
            return Db.Query().CommandText("select count(*) from votes where groupEID=@groupEID")
                          .SetParameter("@groupEID", groupEID)
                          .ExecuteScalar<int>();
        }

        public void DeleteVote(Vote vote)
        {
            var res = Db.Query().CommandText("delete voteentries where voteid=@voteID;delete votes where voteid=@voteID")
                             .SetParameter("@voteID", vote.voteID)
                             .ExecuteNonQuery();

            if ( res == 0 )
                throw new PerpetuumException(ErrorCodes.SQLDeleteError);
        }
        
        public int[] GetMyOpenVotes(Character character, long groupEID)
        {
            return Db.Query().CommandText("select voteid from votes where groupEID=@groupEID and voteid not in (select voteid from voteentries where characterid=@characterID)")
                            .SetParameter("@characterID", character.Id)
                            .SetParameter("@groupEID", groupEID)
                            .Execute().Select(v => v.GetValue<int>(0)).ToArray();
        }
    }
}

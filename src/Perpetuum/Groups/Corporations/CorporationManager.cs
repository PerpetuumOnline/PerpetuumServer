using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.Log;
using Perpetuum.Services.Standing;
using Perpetuum.Threading.Process;
using Perpetuum.Timers;
using Perpetuum.Zones.Intrusion;

namespace Perpetuum.Groups.Corporations
{
    public struct CorporationInviteInfo
    {
        public readonly Character sender;
        public readonly DateTime startInviteTime;

        public CorporationInviteInfo(Character sender)
        {
            this.sender = sender;
            startInviteTime = DateTime.Now;
        }
    }

    public class CorporateInvites
    {
        private readonly ConcurrentDictionary<Character, CorporationInviteInfo> _invites = new ConcurrentDictionary<Character, CorporationInviteInfo>();

        public void InviteCycle()
        {
            foreach (var kvp in _invites)
            {
                if (kvp.Value.startInviteTime.AddMinutes(1) >= DateTime.Now)
                    continue;

                var character = kvp.Key;
                var sender = kvp.Value.sender;

                var d = new Dictionary<string, object>
                    {
                        {k.characterID, character.Id},
                        {k.answer, -1}
                    };

                Message.Builder.SetCommand(Commands.CorporationInviteReply).WithData(d).ToCharacters(sender, character).Send();
                _invites.Remove(character);
            }
        }

        public void AddInvite(Character sender, Character character)
        {
            _invites[character] = new CorporationInviteInfo(sender);
        }

        public void RemoveInvite(Character character)
        {
            _invites.Remove(character);
        }

        public bool TryGetInvite(Character character, out CorporationInviteInfo inviteInfo)
        {
            return _invites.TryGetValue(character, out inviteInfo);
        }

        public bool ContainsInvite(Character character)
        {
            return _invites.Keys.Contains(character);
        }
    }

    public interface ICorporationManager : IProcess
    {
        bool IsStandingMatch(long sourceCorporationEid, long targetCorporationEid, double? standingLimit);
        ErrorCodes IsInJoinOrLeave(Character character);
        bool IsJoinAllowed(Character character);
        Character[] LoadCorporationMembersWithAnyRole(long corporationEid, CorporationRole corporationRole);

        DateTime AddLeaveEntry(Character character);
        DateTime GetLeaveTime(Character character, out bool isLeaveActive);
        DateTime GetJoinEnd(Character member, long corporationEID, out bool isJoinActive);

        void InformCorporationMemberTransferred(Corporation oldCorporation, Corporation newCorporation, Character member);
        void InformCorporationMemberTransferred(Corporation oldCorporation, Corporation newCorporation, Character member, Character kicker);
        bool IsInLeavePeriod(Character character);
        bool IsJoinPeriodExpired(Character member, long corporationEID);
        string GetCorporationNameByMember(Character member);

        IDictionary<string, object> GetYellowPages(long corporationeid);
        void DeleteYellowPages(long corporationEid);

        CorporationRole GetAllRoles();

        CorporationConfiguration Settings { get; }
        void CleanUpCharacterLeave(Character member);

        CorporateInvites Invites { get; }

        IDictionary<string, object> GetCorporationRoleHistory(long corporationEid, int offsetInDays);
        IDictionary<string, object> GetCorporationRoleHistoryOneCharacter(long corporationEid, Character character, int offsetInDays);
    }


    public class CorporationManager : ICorporationManager
    {
        private readonly IStandingHandler _standingHandler;
        private readonly TimerList _timers = new TimerList();

        public CorporationConfiguration Settings { get; }
        public CorporateInvites Invites { get; }

        public CorporationManager(IStandingHandler standingHandler,
                                  CorporationConfiguration corporationConfiguration,
                                  CorporateInvites corporateInvites)
        {
            _standingHandler = standingHandler;
            Settings = corporationConfiguration;
            Invites = corporateInvites;

            _timers.Add(new TimerAction(Invites.InviteCycle, TimeSpan.FromSeconds(3.03), true));
            _timers.Add(new TimerAction(ScheduleCollectRend, TimeSpan.FromHours(1.07))); //not async
            _timers.Add(new TimerAction(FinishLeave, TimeSpan.FromSeconds(7.07), true));
            _timers.Add(new TimerAction(ScheduleIntrusionIncome, TimeSpan.FromHours(5.013))); //not async
        }

        public void Start()
        {
            InitCorporationLeave();
        }

        public void Stop()
        {
        }

        public void Update(TimeSpan time)
        {
            _timers.Update(time);
        }

        private void ScheduleIntrusionIncome()
        {
            IntrusionHelper.DoSiegeCorporationSharePayOutAsync();
        }

        private DateTime _lastCollect = DateTime.Now;

        private void ScheduleCollectRend()
        {
            //run it once a day
            if ((DateTime.Now - _lastCollect).TotalDays < 1)
                return;

            CorporateHangar.CollectHangarRentAsync(_standingHandler).ContinueWith(t =>
            {
                _lastCollect = DateTime.Now;
            });
        }

        private void InitCorporationLeave()
        {
            _leavePeriodMinutes = Settings.LeavePeriod;
        }

        private bool _leaveProcessWorking;

        private int _leavePeriodMinutes;

        private void FinishLeave()
        {
            //this function is still running. ->
            if (_leaveProcessWorking)
                return;

            try
            {
                _leaveProcessWorking = true;

                var records = Db.Query().CommandText("select leavetime,characterid from corporationleave where leavetime<@leavetime")
                    .SetParameter("@leavetime", DateTime.Now.AddMinutes(_leavePeriodMinutes))
                    .Execute();

                foreach (var record in records)
                {
                    try
                    {
                        var leaveDate = record.GetValue<DateTime>(0);
                        var character = Character.Get(record.GetValue<int>(1));
                        if (character == Character.None) 
                            continue;

                        if (leaveDate > DateTime.Now)
                            continue;

                        using (var scope = Db.CreateTransaction())
                        {
                            var currentCorporation = character.GetPrivateCorporation();
                            if (currentCorporation != null)
                            {
                                //leave the corporation and inform
                                currentCorporation.Leave(character);

                                //force remove
                                CleanUpCharacterLeave(character);
                            }

                            scope.Complete();
                        }

                    }
                    catch (Exception ex)
                    {
                        Logger.Error("error occured in finishLeave");
                        Logger.Exception(ex);
                    }
                }

            }
            catch (Exception ex)
            {
                Logger.Error("error occured in finishLeave");
                Logger.Exception(ex);
            }
            finally
            {
                _leaveProcessWorking = false;
            }
        }


        public IDictionary<string, object> GetCorporationRoleHistoryOneCharacter(long corporationEid, Character character, int offsetInDays)
        {
            var later = DateTime.Now.AddDays(-1 * offsetInDays);
            var earlier = later.AddDays(-14);

            var records = Db.Query().CommandText("select * from corporationrolehistory where corporationEID=@corporationEID and memberID=@characterID and rolesettime > @earlier and rolesettime < @later")
                                  .SetParameter("@corporationEID", corporationEid)
                                  .SetParameter("@characterID", character.Id)
                                  .SetParameter("@earlier", earlier)
                                  .SetParameter("@later", later)
                                  .Execute();

            return records.ToDictionary("c", RecordToRoleHistoryEntry);
        }

        public IDictionary<string, object> GetCorporationRoleHistory(long corporationEid, int offsetInDays)
        {
            var later = DateTime.Now.AddDays(-1 * offsetInDays);
            var earlier = later.AddDays(-14);

            var records = Db.Query().CommandText("select * from corporationrolehistory where corporationEID=@corporationEID and rolesettime > @earlier and rolesettime < @later")
                                  .SetParameter("@corporationEID", corporationEid)
                                  .SetParameter("@earlier", earlier)
                                  .SetParameter("@later", later).Execute();

            return records.ToDictionary("c", RecordToRoleHistoryEntry);
        }

        private static Dictionary<string, object> RecordToRoleHistoryEntry(IDataRecord record)
        {
            return new Dictionary<string, object>
            {
                {k.issuerID, record.GetValue<int>("issuerID")},
                {k.memberID, record.GetValue<int>("memberID")},
                {k.oldRole, record.GetValue<int>("oldrole")},
                {k.newRole, record.GetValue<int>("newrole")},
                {k.date, record.GetValue<DateTime>("rolesettime")}
            };
        }

        public Character[] LoadCorporationMembersWithAnyRole(long corporationEid, CorporationRole corporationRole)
        {
            return Db.Query().CommandText("select memberid from corporationmembers where corporationeid=@corpEID and ((role & @roleMask) > 0)")
                           .SetParameter("@corpEID", corporationEid)
                           .SetParameter("@roleMask", corporationRole)
                           .Execute().Select(r => Character.Get(r.GetValue<int>(0))).ToArray();
        }

        public string GetCorporationNameByMember(Character member)
        {
            if (member == Character.None)
                return string.Empty;

            var corporationEid = member.CorporationEid;
            if (corporationEid == 0L)
                return string.Empty;

            return Db.Query().CommandText("select name from corporations where eid = @corporationEid").SetParameter("@corporationEid", corporationEid).ExecuteScalar<string>();
        }

        public IDictionary<string, object> GetYellowPages(long corporationeid)
        {
            var record = Db.Query().CommandText("select * from yellowpages where corporationeid=@corporationeid")
                .SetParameter("@corporationeid",corporationeid)
                .ExecuteSingleRow();

            if (record == null)
                return new Dictionary<string, object>();

            return record.RecordToDictionary();
        }

        public void DeleteYellowPages(long corporationEid)
        {
            Db.Query().CommandText("delete yellowpages where corporationeid=@corporationeid").SetParameter("@corporationeid",corporationEid).ExecuteNonQuery();
        }

        public DateTime GetJoinEnd(Character member, long corporationEID, out bool isJoinActive)
        {
            var joinedTime = GetJoinTime(member, corporationEID, out bool wasEntry);

            if (wasEntry)
            {
                isJoinActive = false;
                return default(DateTime);
            }

            var leaveMinutes = Settings.LeavePeriod;
            joinedTime = joinedTime.AddMinutes(leaveMinutes);
            isJoinActive = (joinedTime >= DateTime.Now);
            return joinedTime;
        }

        private static DateTime GetJoinTime(Character member, long corporationEID)
        {
            return GetJoinTime(member, corporationEID, out bool _);
        }

        private static DateTime GetJoinTime(Character member, long corporationEID, out bool wasEntry)
        {
            wasEntry = true;

            var joinedTime = Db.Query().CommandText("select top (1) corporationJoined from corporationhistory where characterID=@characterID and corporationEID=@corporationEID and corporationLeft is NULL")
                .SetParameter("@characterID", member.Id)
                .SetParameter("@corporationEID", corporationEID)
                .ExecuteScalar<DateTime>();

            if (!joinedTime.Equals(default(DateTime)))
            {
                wasEntry = false;
            }

            return joinedTime;
        }

        public bool IsJoinPeriodExpired(Character member, long corporationEID)
        {
            var joinedTime = GetJoinTime(member, corporationEID);
            return joinedTime.AddMinutes(Settings.LeavePeriod) < DateTime.Now;
        }

        public ErrorCodes IsInJoinOrLeave(Character character)
        {
            if (IsInLeavePeriod(character)) 
                return ErrorCodes.CorporationMemberInLeavePeriod;
            if (!IsJoinPeriodExpired(character,character.CorporationEid)) 
                return ErrorCodes.CorporationCharacterInJoinPeriod;

            return ErrorCodes.NoError;
        }

        public DateTime AddLeaveEntry(Character character)
        {
            var leaveTime = DateTime.Now.AddMinutes(Settings.LeavePeriod);

            Db.Query().CommandText("insert corporationleave (characterid, leavetime) values (@characterID, @leavetime)")
                .SetParameter("@characterID", character.Id)
                .SetParameter("@leavetime", leaveTime)
                .ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLInsertError);

            return leaveTime;
        }

        public void CleanUpCharacterLeave(Character character)
        {
            var res = Db.Query().CommandText("delete corporationleave where characterid=@characterID")
                .SetParameter("@characterID", character.Id)
                .ExecuteNonQuery();

            Logger.Info("characterleave cleanup affected rows:" + res + "  characterID:" + character.Id);
        }

        public bool IsInLeavePeriod(Character character)
        {
            return Db.Query().CommandText("select characterid from corporationleave where characterid=@characterID")
                .SetParameter("@characterID", character.Id)
                .ExecuteScalar<int>() > 0;
        }

        public bool IsJoinAllowed(Character character)
        {
            const string query = @"SELECT TOP(1) corporationjoined FROM dbo.corporationhistory 
                                   WHERE characterID = @characterID AND (SELECT defaultcorp FROM corporations WHERE eid=corporationEID) = 0  
                                   ORDER BY corporationjoined DESC";

            var lastJoinedTime = Db.Query().CommandText(query)
                .SetParameter("@characterID", character.Id)
                .ExecuteScalar<DateTime>();

            if (DateTime.Now.Subtract(lastJoinedTime).TotalMinutes < Settings.LeavePeriod)
                return false;

            return true;
        }

        public DateTime GetLeaveTime(Character character, out bool isLeaveActive)
        {
            var record = Db.Query().CommandText("select leavetime from corporationleave where characterid=@characterID")
                .SetParameter("@characterID", character.Id)
                .ExecuteSingleRow();

            if (record == null)
            {
                isLeaveActive = false;
                return default(DateTime);
            }

            isLeaveActive = true;
            return record.GetValue<DateTime>(0);
        }

        public void InformCorporationMemberTransferred(Corporation oldCorporation, Corporation newCorporation, Character member)
        {
            InformCorporationMemberTransferred(oldCorporation, newCorporation, member, Character.None);
        }

        public void InformCorporationMemberTransferred(Corporation oldCorporation, Corporation newCorporation,Character member,Character kicker)
        {
            var data = new Dictionary<string, object>
            {
                {k.@from, oldCorporation.Eid},
                {k.to, newCorporation.Eid},
                {k.memberID, member.Id}
            };

            if (kicker.Id > 0)
            {
                data.Add(k.kickedBy, kicker.Id);
            }

            if (newCorporation is PrivateCorporation)
            {
                Message.Builder.SetCommand(Commands.CorporationMemberTransferred)
                    .WithData(data)
                    .ToCorporation(newCorporation)
                    .Send();
            }
            else
            {
                Message.Builder.SetCommand(Commands.CorporationMemberTransferred)
                    .WithData(data)
                    .ToCharacter(member)
                    .Send();
            }

            if (oldCorporation is PrivateCorporation)
            {
                Message.Builder.SetCommand(Commands.CorporationMemberTransferred)
                    .WithData(data)
                    .ToCorporation(oldCorporation)
                    .Send();
            }

            CorporationData.RemoveFromCache(newCorporation.Eid);
            CorporationData.RemoveFromCache(oldCorporation.Eid);
        }

        public CorporationRole GetAllRoles()
        {
            var result = 0;
            foreach (var v in Enum.GetValues(typeof(CorporationRole)))
            {
                result = result | (int)v;
            }

            return (CorporationRole)result;
        }

        public bool IsStandingMatch(long sourceCorporationEid, long targetCorporationEid, double? standingLimit)
        {
            if (standingLimit == null || sourceCorporationEid == 0)
                return true;

            //my corp -> ok
            if (sourceCorporationEid == targetCorporationEid)
                return true;

            var standing = _standingHandler.GetStanding(sourceCorporationEid, targetCorporationEid);
            return standing >= standingLimit;
        }
    }

}
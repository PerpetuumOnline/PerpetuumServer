using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Transactions;
using Perpetuum.Accounting.Characters;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Containers;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.GenXY;
using Perpetuum.Groups.Corporations.Loggers;
using Perpetuum.Log;
using Perpetuum.Services.ExtensionService;
using Perpetuum.Services.Insurance;
using Perpetuum.Zones;

namespace Perpetuum.Groups.Corporations
{
    public static class CorporationExtensions
    {
        public static IDictionary<string, object> ToDictionary(this IEnumerable<CorporationMember> members)
        {
            return members.ToDictionary("m", m => m.ToDictionary());
        }

        public static T CheckAccessAndThrowIfFailed<T>(this T corporation,Character member, params CorporationRole[] roles) where T:Corporation
        {
            corporation.IsAnyRole(member, roles).ThrowIfFalse(ErrorCodes.InsufficientPrivileges);
            return corporation;
        }
    }
    
    public abstract partial class Corporation : Entity
    {
        private CorporationMember[] _members;

        public IExtensionReader ExtensionReader { get; set; }
        public ICorporationManager CorporationManager { get; set; }
        public Lazy<IZoneManager> ZoneManager { get; set; }
        public CorporationLogger.Factory CorporationLoggerFactory { get; set; }
        public CorporationTransactionLogger TransactionLogger { get; set; }

        public void LogTransaction(TransactionLogEventBuilder builder)
        {
            LogTransaction(builder.Build());
        }

        public void LogTransaction(TransactionLogEvent e)
        {
            TransactionLogger.Log(e);
        }

        public CorporationLogger GetLogger()
        {
            return CorporationLoggerFactory(this);
        }

        public IEnumerable<Character> GetCharacterMembers()
        {
            return Members.Select(m => m.character).ToArray();
        }

        public virtual CorporationMember[] Members
        {
            get { return LazyInitializer.EnsureInitialized(ref _members, GetMembersFromDb); }
        }

        public bool IsMember(Character character)
        {
            return Members.Any(m => m.character == character);
        }

        private CorporationMember[] GetMembersFromDb()
        {
            return Db.Query().CommandText("select memberid,role from corporationmembers where corporationEID = @corporationEID")
                           .SetParameter("@corporationEID", Eid)
                           .Execute()
                           .Select(r => new CorporationMember
                           {
                               character = Character.Get(r.GetValue<int>(0)),
                               role = (CorporationRole)r.GetValue<int>(1)
                           })
                           .Where(m => m.character != Character.None)
                           .ToArray();
        }

        public virtual void AddMember(Character member, CorporationRole role, Corporation oldCorporation)
        {
            //update history
            WriteMemberHistory(member, oldCorporation);
            member.CorporationEid = Eid;

            Db.Query().CommandText("update corporationmembers set corporationEID = @corporationEID,role = @role where memberid = @memberid")
                .SetParameter("@corporationEID", Eid)
                .SetParameter("@memberid", member.Id)
                .SetParameter("@role", role)
                .ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLUpdateError);

            _members = null;
        }

        public void RemoveMember(Character member)
        {
            _members = null;
            OnMemberRemoved(member);
        }

        protected virtual void OnMemberRemoved(Character member)
        {
        }

        private CorporationMember GetMember(Character member)
        {
            return Members.FirstOrDefault(m => m.character == member);
        }

        public CorporationRole GetMemberRole(Character member)
        {
            return GetMember(member).role;
        }

        public void SetMemberRole(Character member, CorporationRole newRole)
        {
            Db.Query().CommandText("update corporationmembers set role = @role where memberid = @memberid and corporationEID = @corporationEID")
                .SetParameter("@role", (int) newRole)
                .SetParameter("@corporationEID", Eid)
                .SetParameter("@memberid", member.Id)
                .ExecuteNonQuery().ThrowIfNotEqual(1,ErrorCodes.SQLUpdateError);

            _members = null;

            Transaction.Current.OnCommited(() =>
            {
                OnMemberRoleUpdated(member,newRole);

                Message.Builder.SetCommand(Commands.CorporationSetMemberRole)
                    .WithData(new Dictionary<string, object> { { k.memberID, member.Id }, { k.newRole, (int)newRole } })
                    .ToCharacters(GetCharacterMembers())
                    .Send();

               
                ZoneManager.Value.Zones.ForEach(z => z.UpdateCorporation(CorporationCommand.ChangeRole,new Dictionary<string,object>
                            {
                                {k.characterID, member.Id},
                                {k.role, (int) newRole},
                                {k.corporationEID, Eid}
                            }));
            });
        }

        protected virtual void OnMemberRoleUpdated(Character member, CorporationRole newRole)
        {
           
        }

        public IEnumerable<Character> GetMembersByRole(CorporationRole role)
        {
            return Members.Where(m => ((int) m.role & (int) role) > 0).Select(m => m.character);
        }

        public IDictionary<string, object> GetMembersWithAnyRoleToDictionary(params CorporationRole[] roles)
        {
            return GetMembersWithAnyRoles(roles).ToDictionary();
        }

        public IEnumerable<CorporationMember> GetMembersWithAnyRoles(params CorporationRole[] roles)
        {
            return Members.Where(m => m.role.IsAnyRole(roles));
        }

        public bool CanSetTaxRate(Character member)
        {
            return IsAnyRole(member, CorporationRole.Accountant, CorporationRole.CEO, CorporationRole.DeputyCEO);
        }

        public int TaxRate
        {
            get { return ReadValueFromDb<int>("taxrate").Clamp(0,100); }
            set { WriteValueToDb("taxrate",value.Clamp(0,100)); }
        }

        private T ReadValueFromDb<T>(string column)
        {
            return Db.Query().CommandText("select "+column+" from corporations where eid=@eid")
                           .SetParameter("@eid", Eid)
                           .ExecuteScalar<T>();
        }

        public string CorporationName
        {
            get { return ReadValueFromDb<string>("name"); }
            set { WriteValueToDb("name", value); }
        }

        public string CorporationNick
        {
            get { return ReadValueFromDb<string>("nick"); }
            set { WriteValueToDb("nick", value); }
        }



        private void WriteValueToDb(string column,object value)
        {
            Db.Query().CommandText("update corporations set "+column+"=@value where eid=@eid")
                .SetParameter("@eid", Eid)
                .SetParameter("@value", value)
                .ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLUpdateError);
        }

        public void SetPrivateProfile(Dictionary<string, object> privateProfile)
        {
            WriteValueToDb("privateProfile",GenxyConverter.Serialize(privateProfile));
        }

        public void SetPublicProfile(Dictionary<string, object> publicProfile)
        {
            WriteValueToDb("publicProfile",GenxyConverter.Serialize(publicProfile));
        }

        public bool HasRole(Character member, CorporationRole role)
        {
            return GetMemberRole(member).HasRole(role);
        }

        public bool HasAllRoles(Character member, params CorporationRole[] roles)
        {
            return GetMemberRole(member).HasAllRoles(roles);
        }

        public bool IsAnyRole(Character member, params CorporationRole[] roles)
        {
            return GetMemberRole(member).IsAnyRole(roles);
        }

        public Character CEO
        {
            get
            {
                var member = Members.FirstOrDefault(m => (((int) m.role & (int) CorporationRole.CEO) > 0));
                return member.character;
            }
        }

        protected int MaxMemberCount
        {
            get
            {
                var ceos = GetMembersByRole(CorporationRole.CEO).ToArray();

                if (ceos.Length == 0)
                {
                    Logger.Error("No CEO was found for corporation: " + Eid);
                    return 1;
                }

                var ceo = ceos.FirstOrDefault();
                return GetMaxmemberByCharacter(ceo);
            }
        }

        public int GetMaxmemberByCharacter(Character character)
        {
            var enablerExtension = ED.EnablerExtensions.Keys.FirstOrDefault();
            if (enablerExtension.id <= 0)
            {
                Logger.Error("no enabler extension defined for corporation.");
                return 1; //problem solved
            }

            return (int)character.GetExtensionBonusWithPrerequiredExtensions(enablerExtension.id);
        }

        public int MembersCount
        {
            get { return Members.Length; }
        }

        public bool IsAvailableFreeSlot
        {
            get { return (Members.Length < MaxMemberCount); }
        }

        public bool IsActive
        {
            get { return ReadValueFromDb<bool>("active"); }
            set { WriteValueToDb("active", value); }
        }

        public virtual IDictionary<string, object> GetInfoDictionaryForMember(Character member)
        {
            return Description.ToDictionary();
        }

        private CorporationDescription _description;

        public CorporationDescription Description
        {
            get { return LazyInitializer.EnsureInitialized(ref _description, () => CorporationDescription.Get(Eid)); }
        }

        public override void OnDeleteFromDb()
        {
            //$$$ memberek?
            Db.Query().CommandText("delete from corporations where eid = @corporationEID")
                .SetParameter("@corporationEID", Eid)
                .ExecuteNonQuery().ThrowIfEqual(0,ErrorCodes.SQLDeleteError);

            base.OnDeleteFromDb();
        }
       
        private void WriteMemberHistory(Character character,Corporation oldCorporation)
        {
            //old corp
            Db.Query().CommandText("update corporationHistory set corporationLeft=@now where characterID=@characterID and corporationLeft is NULL and corporationeid=@oldCorpEID")
                    .SetParameter("@characterID", character.Id)
                    .SetParameter("@now", DateTime.Now)
                    .SetParameter("@oldCorpEID", oldCorporation.Eid)
                    .ExecuteNonQuery();

            //new corp
            Db.Query().CommandText("insert corporationHistory (characterID, corporationEID, corporationJoined) values (@characterID, @corporationEID, @now)")
                    .SetParameter("@characterID", character.Id)
                    .SetParameter("@corporationEID", Eid)
                    .SetParameter("@now", DateTime.Now)
                    .ExecuteNonQuery();
        }

        public void WriteRoleHistory(Character issuer,Character member, CorporationRole newrole, CorporationRole oldrole)
        {
            if (newrole == oldrole) 
                return;

            Db.Query().CommandText("insert corporationrolehistory (corporationEID,issuerID,memberID,oldrole,newrole) values (@corporationEID,@issuerID,@memberID,@oldrole,@newrole)")
                    .SetParameter("@corporationEID", Eid)
                    .SetParameter("@issuerID", issuer.Id)
                    .SetParameter("@memberID", member.Id)
                    .SetParameter("@newrole", (int) newrole)
                    .SetParameter("@oldrole", (int) oldrole)
                    .ExecuteNonQuery();
        }

        public string ChannelName
        {
            get { return $"corporation_{Eid}"; }
        }

        public void SendInsuranceList()
        {
            var corpInfo = InsuranceHelper.GetCorporationInsurances(Eid);

            var affectedMembers = GetMembersWithAnyRoles(CorporationRole.CEO, CorporationRole.DeputyCEO, CorporationRole.Accountant).Select(r => r.character).ToArray();

            if (corpInfo.Count > 0)
            {
                Message.Builder.SetCommand(Commands.ProductionCorporationInsuranceList).WithData(corpInfo).ToCharacters(affectedMembers).Send();
            }
            else
            {
                var emptyDict = new Dictionary<string, object> {{k.state, k.empty}};
                Message.Builder.SetCommand(Commands.ProductionCorporationInsuranceList).WithData(emptyDict).ToCharacters(affectedMembers).Send();
            }
        }

        public void cache_addOrUpdateMember(int characterId, CorporationRole role)
        {
            _members = null;
        }

        public void cache_removeMember(int characterId)
        {
            _members = null;
        }

        public void SetColor(int color)
        {
            Db.Query().CommandText("update corporations set color=@color where eid=@eid")
                .SetParameter("@color", color)
                .SetParameter("@eid", Eid)
                .ExecuteNonQuery().ThrowIfEqual(0,ErrorCodes.SQLUpdateError);
        }

        public CorporateHangar GetHangar(long hangarEid,Character character,ContainerAccess containerAccess = ContainerAccess.Delete)
        {
            var role = GetMemberRole(character);

            var hangar = (CorporateHangar)Container.GetOrThrow(hangarEid);

            if (!role.IsAnyRole(CorporationRole.CEO, CorporationRole.DeputyCEO))
            {
                role.IsAnyRole(CorporationRole.HangarOperator).ThrowIfFalse(ErrorCodes.InsufficientPrivileges);
                hangar.CheckAccessAndThrowIfFailed(character, containerAccess);
            }

            hangar.Owner.ThrowIfNotEqual(Eid, ErrorCodes.AccessDenied);
            return hangar;
        }

        public void CheckMaxMemberCountAndThrowIfFailed(Character desiredCeoCharacter)
        {
            var desiredCEOMaxMembers = GetMaxmemberByCharacter(desiredCeoCharacter);
            MembersCount.ThrowIfGreater(desiredCEOMaxMembers, ErrorCodes.CorporationMaxMembersMismatch);
        }

        public void CheckCeoLastActivityAndThrowIfFailed()
        {
#if DEBUG
            DateTime.Now.Subtract(CEO.LastLogout).TotalMinutes.ThrowIfLess(1,ErrorCodes.CEOWasActiveRecently);
#else
            //elesen 3 het
            DateTime.Now.Subtract(CEO.LastLogout).TotalDays.ThrowIfLess(30,ErrorCodes.CEOWasActiveRecently);
#endif
        }

        public static bool Exists(long eid)
        {
            return Db.Query().CommandText("select count(*) from corporations where eid=@corpEID")
                           .SetParameter("@corpEID", eid)
                           .ExecuteScalar<int>() == 1;
        }

        public static bool IsNameOrNickTaken(string name, string nick, bool checkHistory = true)
        {
            if (checkHistory)
            {
                var inHistory =
                    Db.Query().CommandText("select count(*) from corporationnamehistory where [name]=@name or nick=@nick")
                            .SetParameter("@name", name)
                            .SetParameter("@nick", nick)
                            .ExecuteScalar<int>();

                if (inHistory > 0)
                {
                    return true;
                }
            }

            return Db.Query().CommandText("select count(*) from corporations where [name]=@name or nick=@nick")
                           .SetParameter("@name", name)
                           .SetParameter("@nick", nick)
                           .ExecuteScalar<int>() > 0;
        }

        public static CorporationRole GetRoleFromSql(Character character)
        {
            return (CorporationRole)Db.Query().CommandText("select role from corporationmembers where memberid=@characterID")
                                            .SetParameter("@characterID", character.Id)
                                            .ExecuteScalar<int>();
        }


        /// <summary>
        /// this must be the quickest way to get the corpEid and role. 
        /// 
        /// often only these two are needed... 
        /// zoneGetBuildings and other requests use this. 
        /// 
        /// 
        /// </summary>
        /// <param name="character"></param>
        /// <param name="corporationEid"></param>
        /// <param name="role"></param>
        /// <returns></returns>
        public static ErrorCodes GetCorporationEidAndRoleFromSql(Character character, out long corporationEid, out CorporationRole role)
        {
            corporationEid = 0;
            role = CorporationRole.NotDefined;

            var record = Db.Query().CommandText("select corporationEID,role from dbo.getCorporationEidAndRole(@characterID)")
                .SetParameter("@characterID", character.Id)
                .ExecuteSingleRow();

            if (record == null) return ErrorCodes.CharacterNotFound;

            corporationEid = record.GetValue<long>(0);
            role = (CorporationRole) record.GetValue<int>(1);


            return ErrorCodes.NoError;

        }


        public static Corporation GetByName(string name)
        {
            var eid = GetEidByName(name);
            return GetOrThrow(eid);
        }

        public static long GetEidByName(string name)
        {
            return Db.Query().CommandText("select eid from corporations where name=@name")
                           .SetParameter("@name",name)
                           .ExecuteScalar<long>();
        }

        public void SetName(string name, string nick)
        {
            WriteValueToDb("name",name);
            WriteValueToDb("nick", nick);
        }

        public static IDictionary<string,object> ListPreviousAliasesToDictionary(long corporationEid)
        {
            var counter = 0;
            var result = new Dictionary<string, object>()
                {
                    {k.corporationEID, corporationEid},
                    {k.aliases, ListPreviousAliases(corporationEid).ToDictionary<CorporationAlias, string, object>(o => "c" + counter++, o => o.ToDictionary())}
                };


            return result;
        }


        private static IEnumerable<CorporationAlias> ListPreviousAliases(long corporationEid)
        {
            return 
                Db.Query().CommandText("select * from corporationnamehistory where corporationeid=@eid")
                .SetParameter("@eid", corporationEid)
                .Execute()
                .Select(r => new CorporationAlias(r));
        }

        public static void WriteRenameHistory(long corporationEid,  Character character, string corpName, string nick)
        {
            Db.Query().CommandText("insert corporationnamehistory (corporationeid,name,nick,characterid) values (@corpEid,@name,@nick,@characterId)")
                    .SetParameter("@corpEid", corporationEid)
                    .SetParameter("@name", corpName)
                    .SetParameter("@nick", nick)
                    .SetParameter("@characterId", character.Id)
                    .ExecuteNonQuery();
        }

        public int GetMaximumRegisteredProbesAmount()
        {
            var extension = ExtensionReader.GetExtensionByName(ExtensionNames.VISIBILITY_PROBE_USER);
            Debug.Assert(extension != null, "extension != null");
            return 10 + CEO.GetExtensionLevel(extension.id) * (int)extension.bonus;
        }

        public IDictionary<string, object> GetTransactionHistory(int offsetInDays)
        {
            var later = DateTime.Now.AddDays(-1 * offsetInDays);
            var earlier = later.AddDays(-2);

            const string selectCmdText = @"select * from corporationtransactions 
                                           where corporationeid=@corporationEid and 
                                           transactiondate > @earlier and 
                                           transactiondate < @later";

            return Db.Query().CommandText(selectCmdText)
                .SetParameter("@corporationEid",Eid)
                .SetParameter("@earlier", earlier)
                .SetParameter("@later", later).Execute()
                .ToDictionary("c", r =>
                {
                    var result = new Dictionary<string, object>
                    {
                        {k.transactionType, r.GetValue<int>("transactiontype")}, 
                        {k.amount, r.GetValue<double>("amount")}, 
                        {k.date, r.GetValue<DateTime>("transactiondate")}, 
                        {k.wallet, (long) r.GetValue<double>("currentwallet")}
                    };

                    if (!r.IsDBNull("quantity") && !r.IsDBNull("definition"))
                    {
                        result.Add(k.quantity, r.GetValue<int>("quantity"));
                        result.Add(k.definition, r.GetValue<int>("definition"));
                    }

                    if (!r.IsDBNull("memberid")) { result.Add(k.memberID, r.GetValue<int>("memberid")); }
                    if (!r.IsDBNull("targetmemberid")) { result.Add(k.target, r.GetValue<int>("targetmemberid")); }
                    if (!r.IsDBNull("involvedCorporationEID")) { result.Add(k.involvedCorporation, r.GetValue<long>("involvedCorporationEID")); }

                    return result;
                });
        }

        public int GetBoardMembersCount()
        {
            return GetMembersWithAnyRoles(CorporationRole.CEO, CorporationRole.DeputyCEO).Count();
        }

        public static IDictionary<string,object> GetCorporationHistory(Character character)
        {
            var history = Db.Query().CommandText("select corporationEID, corporationJoined, corporationLeft from corporationHistory where characterID=@characterID")
                .SetParameter("@characterID",character.Id)
                .Execute()
                .ToDictionary("e",r =>
                {
                    var oneEntry = new Dictionary<string,object>
                    {
                        {k.corporationEID, r.GetValue<long>(0)},
                        {k.joined, r.GetValue<DateTime>(1)}
                    };

                    if (!r.IsDBNull(2))
                    {
                        oneEntry.Add(k.left,r.GetValue<DateTime>(2));
                    }

                    return oneEntry;
                });

            return new Dictionary<string,object>
            {
                { k.characterID,character.Id },
                { k.history, history }
            };
        }


    }
}
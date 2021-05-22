using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Caching;
using System.Threading.Tasks;
using System.Transactions;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Containers;
using Perpetuum.Data;
using Perpetuum.GenXY;
using Perpetuum.Groups.Alliances;
using Perpetuum.Groups.Corporations;
using Perpetuum.Groups.Gangs;
using Perpetuum.Items;
using Perpetuum.Log;
using Perpetuum.Players;
using Perpetuum.Robots;
using Perpetuum.Services.ExtensionService;
using Perpetuum.Services.MissionEngine.TransportAssignments;
using Perpetuum.Services.Social;
using Perpetuum.Services.TechTree;
using Perpetuum.Units.DockingBases;
using Perpetuum.Wallets;
using Perpetuum.Zones;

namespace Perpetuum.Accounting.Characters
{
    public delegate Character CharacterFactory(int id);

    public class Character :  IEquatable<Character>,IComparable<Character>
    {
        private static Character _none;

        public static Character None => _none ?? (_none = CharacterFactory(0));

        public static bool IsSystemCharacter(Character c)
        {
            return c.Nick.Contains("[OPP]");  //TODO better configuration of system characters to avoid flimsy name rule
        }

        private readonly IAccountManager _accountManager;
        private readonly Lazy<IZoneManager> _zoneManager;
        private readonly DockingBaseHelper _dockingBaseHelper;
        private readonly RobotHelper _robotHelper;
        private readonly ICharacterTransactionLogger _transactionLogger;
        private readonly ICharacterExtensions _characterExtensions;
        private readonly IExtensionReader _extensionReader;
        private readonly ISocialService _socialService;
        private readonly ICorporationManager _corporationManager;
        private readonly ITechTreeService _techTreeService;
        private readonly IGangManager _gangManager;
        private readonly CharacterWalletHelper _walletHelper;

        private const string FIELD_CHARACTER_ID = "characterid";
        private const string FIELD_ACCOUNT_ID = "accountid";
        private const string FIELD_ROOT_EID = "rooteid";
        private const string FIELD_CORPORATION_EID = "corporationeid";
        private const string FIELD_DEFAULT_CORPORATION_EID = "defaultcorporationeid";
        private const string FIELD_ALLIANCE_EID = "allianceeid";
        private const string FIELD_ACTIVE_CHASSIS = "activechassis";
        private const string FIELD_BASE_EID = "baseeid";
        private const string FIELD_HOME_BASE_EID = "homebaseeid";
        private const string FIELD_NICK = "nick";
        private const string FIELD_OFFENSIVE_NICK = "offensivenick";
        private const string FIELD_NICK_CORRECTED = "nickcorrected";
        private const string FIELD_DOCKED = "docked";
        private const string FIELD_BLOCK_TRADES = "blocktrades";
        private const string FIELD_GLOBAL_MUTE = "globalmute";
        private const string FIELD_CREDIT = "credit";
        private const string FIELD_MAJOR_ID = "majorId";
        private const string FIELD_RACE_ID = "raceId";
        private const string FIELD_SCHOOL_ID = "schoolId";
        private const string FIELD_SPARK_ID = "sparkId";
        private const string FIELD_AVATAR = "avatar";
        private const string FIELD_LAST_LOGOUT = "lastlogout";
        private const string FIELD_LAST_USED = "lastused";
        private const string FIELD_LANGUAGE = "language";
        private const string FIELD_IN_USE = "inuse";
        private const string FIELD_ZONE_ID = "zoneid";
        private const string FIELD_POSITION_X = "positionx";
        private const string FIELD_POSITION_Y = "positiony";
        private const string FIELD_TOTAL_MINS_ONLINE = "totalminsonline";
        private const string FIELD_MOOD_MESSAGE = "moodmessage";
        private const string FIELD_ACTIVE = "active";
        private const string FIELD_DELETED_AT = "deletedat";
        private const string CACHE_KEY_ID_TO_EID = "id_to_eid_";
        private const string CACHE_KEY_EID_TO_ID = "eid_to_id_";
        private const string CACHE_KEY_ID_TO_ACCOUNTID = "id_to_accountid_";

        public static ObjectCache CharacterCache { get; set; }
      
        public static CharacterFactory CharacterFactory { get; set; }

        public Character()
        {
            
        }

        public Character(int id,IAccountManager accountManager,
                                Lazy<IZoneManager> zoneManager,
                                DockingBaseHelper dockingBaseHelper,
                                RobotHelper robotHelper,
                                ICharacterTransactionLogger transactionLogger,
                                ICharacterExtensions characterExtensions,
                                IExtensionReader extensionReader,
                                ISocialService socialService,
                                ICorporationManager corporationManager,
                                ITechTreeService techTreeService,
                                IGangManager gangManager,
                                CharacterWalletHelper walletHelper)
        {
            _accountManager = accountManager;
            _zoneManager = zoneManager;
            _dockingBaseHelper = dockingBaseHelper;
            _robotHelper = robotHelper;
            _transactionLogger = transactionLogger;
            _characterExtensions = characterExtensions;
            _extensionReader = extensionReader;
            _socialService = socialService;
            _corporationManager = corporationManager;
            _techTreeService = techTreeService;
            _gangManager = gangManager;
            _walletHelper = walletHelper;

            if (id <= 0)
                id = 0;

            Id = id;
        }

        public int Id { get; }

        public long Eid => GetEid(Id);

        public bool IsDocked
        {
            get => ReadValueFromDb<bool>(FIELD_DOCKED);
            set => WriteValueToDb(FIELD_DOCKED, value);
        }

        public int AccountId
        {
            get => GetCachedAccountId(Id);
            set
            {
                WriteValueToDb(FIELD_ACCOUNT_ID, value);
                RemoveAccountIdFromCache();
            }
        }

        public Account GetAccount()
        {
            return _accountManager.Repository.Get(AccountId);
        }

        public string Nick
        {
            get => ReadValueFromDb<string>(FIELD_NICK);
            set => WriteValueToDb(FIELD_NICK, value);
        }

        public bool IsOffensiveNick
        {
            get => ReadValueFromDb<bool>(FIELD_OFFENSIVE_NICK);
            set
            {
                WriteValueToDb(FIELD_OFFENSIVE_NICK,value);
                WriteValueToDb(FIELD_NICK_CORRECTED, !value);
            }
        }

        public double Credit
        {
            get => ReadValueFromDb<double>(FIELD_CREDIT);
            set => WriteValueToDb(FIELD_CREDIT, value);
        }

        public int MajorId
        {
            get => ReadValueFromDb<int>(FIELD_MAJOR_ID);
            set => WriteValueToDb(FIELD_MAJOR_ID, value);
        }

        public int RaceId
        {
            get => ReadValueFromDb<int>(FIELD_RACE_ID);
            set => WriteValueToDb(FIELD_RACE_ID, value);
        }

        public int SchoolId
        {
            get => ReadValueFromDb<int>(FIELD_SCHOOL_ID);
            set => WriteValueToDb(FIELD_SCHOOL_ID, value);
        }

        public int SparkId
        {
            get => ReadValueFromDb<int>(FIELD_SPARK_ID);
            set => WriteValueToDb(FIELD_SPARK_ID, value);
        }

        public int Language
        {
            get => ReadValueFromDb<int>(FIELD_LANGUAGE);
            set => WriteValueToDb(FIELD_LANGUAGE, value);
        }

        public bool IsActive
        {
            get => ReadValueFromDb<bool>(FIELD_ACTIVE);
            set
            {
                WriteValueToDb(FIELD_ACTIVE, value);

                if (!value)
                {
                    DeletedAt = DateTime.Now;
                }
            }
        }

        public DateTime DeletedAt
        {
            get => ReadValueFromDb<DateTime>(FIELD_DELETED_AT);
            set => WriteValueToDb(FIELD_DELETED_AT, value);
        }

        public bool IsOnline
        {
            get => ReadValueFromDb<bool>(FIELD_IN_USE);
            set => WriteValueToDb(FIELD_IN_USE, value);
        }

        public DateTime LastLogout
        {
            get => ReadValueFromDb<DateTime>(FIELD_LAST_LOGOUT);
            set => WriteValueToDb(FIELD_LAST_LOGOUT, value);
        }

        public DateTime LastUsed
        {
            set => WriteValueToDb(FIELD_LAST_USED, value);
        }

        public TimeSpan TotalOnlineTime
        {
            get => TimeSpan.FromMinutes(ReadValueFromDb<int>(FIELD_TOTAL_MINS_ONLINE));
            set => WriteValueToDb(FIELD_TOTAL_MINS_ONLINE, (int)value.TotalMinutes);
        }

        public GenxyString Avatar
        {
            set => WriteValueToDb(FIELD_AVATAR, value.ToString());
        }

        public string MoodMessage
        {
            get => ReadValueFromDb<string>(FIELD_MOOD_MESSAGE);
            set
            {
                if (!string.IsNullOrEmpty(value) && value.Length > 2000)
                {
                    value = value.Substring(0, 1999);
                }

                WriteValueToDb(FIELD_MOOD_MESSAGE, value);
            }
        }

        public int? ZoneId
        {
            get => ReadValueFromDb<int?>(FIELD_ZONE_ID);
            set => WriteValueToDb(FIELD_ZONE_ID, value);
        }

        public Position? ZonePosition
        {
            get
            {
                var x = ReadValueFromDb<double?>(FIELD_POSITION_X);
                var y = ReadValueFromDb<double?>(FIELD_POSITION_Y);

                if (x == null || y == null)
                    return null;

                return new Position((double) x,(double) y);
            }
            set
            {
                double? x = null;
                double? y = null;

                if (value != null)
                {
                    var p = (Position) value;
                    x = p.X;
                    y = p.Y;
                }

                WriteValueToDb(FIELD_POSITION_X,x);
                WriteValueToDb(FIELD_POSITION_Y,y);
            }
        }

        public long CorporationEid
        {
            get => ReadValueFromDb<long>(FIELD_CORPORATION_EID);
            set
            {
                WriteValueToDb(FIELD_CORPORATION_EID, value);

                Db.Query().CommandText("update entities set parent=@corporationEID where eid=@characterEID")
                    .SetParameter("@corporationEID", value)
                    .SetParameter("@characterEID", Eid)
                    .ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLUpdateError);
            }
        }

        public long DefaultCorporationEid
        {
            get => ReadValueFromDb<long>(FIELD_DEFAULT_CORPORATION_EID);
            set => WriteValueToDb(FIELD_DEFAULT_CORPORATION_EID,value);
        }

        public long AllianceEid
        {
            get => ReadValueFromDb<long?>(FIELD_ALLIANCE_EID) ?? 0L;
            set => WriteValueToDb(FIELD_ALLIANCE_EID,value == 0L ? (object) null : value);
        }

        public long ActiveRobotEid
        {
            get => ReadValueFromDb<long?>(FIELD_ACTIVE_CHASSIS) ?? 0;
            set => WriteValueToDb(FIELD_ACTIVE_CHASSIS,value == 0L ? (object)null : value);
        }

        public long CurrentDockingBaseEid
        {
            get => ReadValueFromDb<long>(FIELD_BASE_EID);
            set => WriteValueToDb(FIELD_BASE_EID,value);
        }

        public DateTime NextAvailableUndockTime
        {
            get => GetValueFromCache<DateTime>("nextavailableundocktime");
            set => SetValueToCache("nextavailableundocktime", value);
        }

        public DateTime NextAvailableRobotRequestTime
        {
            get => GetValueFromCache<DateTime>("nextavailablerobotrequesttime");
            set => SetValueToCache("nextavailablerobotrequesttime", value);
        }

        public bool BlockTrades
        {
            get => ReadValueFromDb<bool>(FIELD_BLOCK_TRADES);
            set => WriteValueToDb(FIELD_BLOCK_TRADES, value);
        }

        public bool GlobalMuted
        {
            get => ReadValueFromDb<bool>(FIELD_GLOBAL_MUTE);
            set => WriteValueToDb(FIELD_GLOBAL_MUTE, value);
        }

        public AccessLevel AccessLevel => _accountManager.Repository.GetAccessLevel(GetCachedAccountId(Id));

        public long? HomeBaseEid
        {
            get => ReadValueFromDb<long?>(FIELD_HOME_BASE_EID);
            set => WriteValueToDb(FIELD_HOME_BASE_EID,value);
        }

        private static string GetCacheKey(string prefix,object key)
        {
            return $"{prefix}_{key}";
        }

        public void RemoveFromCache()
        {
            CharacterCache.Remove(GetCacheKey(CACHE_KEY_ID_TO_EID, Id));
            CharacterCache.Remove(GetCacheKey(CACHE_KEY_EID_TO_ID, Eid));
            RemoveAccountIdFromCache();
        }

        private void RemoveAccountIdFromCache()
        {
            CharacterCache.Remove(GetCacheKey(CACHE_KEY_ID_TO_ACCOUNTID, Id));
        }

        public override int GetHashCode()
        {
            return Id;
        }

        public int CompareTo(Character other)
        {
            return Id.CompareTo(other?.Id);
        }
        
        public override bool Equals(object obj)
        {
            var other = obj as Character;
            return other != Character.None && Equals(other);
        }

        public bool Equals(Character other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return Id == other.Id;
        }

        public static bool operator ==(Character left, Character right)
        {
            if (ReferenceEquals(left, right))
                return true;

            if ((object)left == null || (object)right == null)
                return false;

            return left.Equals(right);
        }

        public static bool operator !=(Character left, Character right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            return $"{Id}";
        }

        private string CreateCacheKey(string key)
        {
            return $"character_{key}_{Id}";
        }

        private T GetValueFromCache<T>(string key)
        {
            return CharacterCache.Get(CreateCacheKey(key),() => default(T));
        }

        private void SetValueToCache<T>(string key, T value)
        {
            CharacterCache.Set(CreateCacheKey(key), value);
        }

        private T ReadValueFromDb<T>(string name)
        {
            return ReadValueFromDb<T>(Id, name);
        }

        private void WriteValueToDb(string name, object value)
        {
            WriteValueToDb(Id, name, value);
        }

        #region Helpers

        private static T ReadValueFromDb<T>(int id, string name)
        {
            if (id == 0)
                return default(T);

            return Db.Query().CommandText("select " + name + " from characters where characterid = @id").SetParameter("@id",id).ExecuteScalar<T>();
        }

        private static T ReadValueFromDb<T>(long eid, string name)
        {
            if (eid == 0)
                return default(T);

            return Db.Query().CommandText("select " + name + " from characters where rooteid = @eid").SetParameter("@eid",eid).ExecuteScalar<T>();
        }

        private static void WriteValueToDb(int id, string name, object value)
        {
            if ( id == 0 )
                return;

            Db.Query().CommandText("update characters set " + name + " = @value where characterid = @id")
                .SetParameter("@id",id)
                .SetParameter("@value",value)
                .ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLUpdateError);
        }

        [NotNull]
        public static Character GetByEid(long characterEid)
        {
            if (characterEid == 0L)
                return None;

            var characterId = GetIdByEid(characterEid);
            return CharacterFactory(characterId);
        }

        [NotNull]
        public static Character Get(int id)
        {
            return CharacterFactory(id);
        }

        private static int GetCachedAccountId(int id)
        {
            if (id == 0)
                return 0;

            return CharacterCache.Get(GetCacheKey(CACHE_KEY_ID_TO_ACCOUNTID,id), () => ReadValueFromDb<int>(id,FIELD_ACCOUNT_ID));
        }

        private static long GetEid(int id)
        {
            if (id == 0)
                return 0L;

            return CharacterCache.Get(GetCacheKey(CACHE_KEY_ID_TO_EID, id), () => ReadValueFromDb<long>(id, FIELD_ROOT_EID));
        }

        public static int GetIdByEid(long eid)
        {
            return CharacterCache.Get(GetCacheKey(CACHE_KEY_EID_TO_ID, eid), () => ReadValueFromDb<int>(eid,FIELD_CHARACTER_ID));
        }

        public static bool Exists(int id)
        {
            return id != 0 && ReadValueFromDb<int>(id, FIELD_CHARACTER_ID) > 0;
        }

        public static void CheckNickAndThrowIfFailed(string nick, AccessLevel accessLevel,Account issuerAccount)
        {
            nick = nick.Trim();
            nick.Length.ThrowIfLess(3, ErrorCodes.NickTooShort);
            nick.Length.ThrowIfGreater(25, ErrorCodes.NickTooLong);
            nick.AllowAscii().ThrowIfFalse(ErrorCodes.OnlyAsciiAllowed);

            if (!accessLevel.IsAdminOrGm())
            {
                nick.IsNickAllowedForPlayers().ThrowIfFalse(ErrorCodes.NickReservedForDevelopersAndGameMasters);
            }

            //check history 
            var inHistory =
            Db.Query().CommandText("select count(*) from characternickhistory where nick=@nick and accountid != @accountID")
                    .SetParameter("@accountID", issuerAccount.Id)
                    .SetParameter("@nick", nick)
                    .ExecuteScalar<int>();
            (inHistory > 0).ThrowIfTrue(ErrorCodes.NickTaken);


            //is nick belongs to an active of inavtive character
            var owner = GetByNick(nick);
            if (owner == Character.None)
                return;

            // ok, now we know that the nick is used in the characters table, lets check ownership and timeouts!

            // an active character has this nick
            owner.IsActive.ThrowIfTrue(ErrorCodes.NickTaken);

            //if the character is deleted and belongs to the issuer then it's ok
            var b = owner.AccountId != issuerAccount.Id;
            if (b && !(DateTime.Now.Subtract(owner.DeletedAt).TotalDays > 1))
            {
                throw new PerpetuumException(ErrorCodes.NickTaken);
            }

            //there is a deleted character with the given nick which belongs to this account
            owner.Nick = nick + "_renamed_" + FastRandom.NextString(7);
        }

        [UsedImplicitly]
        public static IEnumerable<Character> GetCharactersDockedInBase(long baseEid)
        {
            return Db.Query().CommandText("select characterid from characters where docked=1 and baseEID=@baseEID and active=1 and inUse=1")
                .SetParameter("@baseEID", baseEid)
                .Execute().Select(r => Get((int) r.GetValue<int>(0)));
        }

        [UsedImplicitly]
        public static Character GetByNick(string nick)
        {
            var id = Db.Query().CommandText("select characterid from characters where nick = @nick").SetParameter("@nick",nick).ExecuteScalar<int>();
            return Get(id);
        }

        #endregion

        public void LogTransaction(TransactionLogEventBuilder builder)
        {
            LogTransaction(builder.Build());
        }

        public void LogTransaction(TransactionLogEvent e)
        {
            _transactionLogger.Log(e);
        }

        public IDictionary<string, object> GetTransactionHistory(int offsetInDays)
        {
            var later = DateTime.Now.AddDays(-offsetInDays);
            var earlier = later.AddDays(-2);

            const string sqlCmd = @"SELECT transactionType,
                                           amount,
                                           transactiondate as date,
                                           currentcredit as credit,
                                           otherCharacter,
                                           quantity,
                                           definition
                                    FROM charactertransactions 
                                    WHERE characterid = @characterId AND transactiondate between @earlier AND @later and amount != 0";

            var result = Db.Query().CommandText(sqlCmd)
                .SetParameter("@characterId", Id)
                .SetParameter("@earlier", earlier)
                .SetParameter("@later", later)
                .Execute()
                .RecordsToDictionary("c");

            return result;
        }

        public bool IsInTraining()
        {
            return RaceId == 0 || SchoolId == 0;
        }

        public void CheckNextAvailableUndockTimeAndThrowIfFailed()
        {
            var nextAvailableUndockTime = NextAvailableUndockTime;
            nextAvailableUndockTime.ThrowIfGreater(DateTime.Now, ErrorCodes.DockingTimerStillRunning, gex => gex.SetData("nextAvailable", nextAvailableUndockTime));
        }

        public void SendErrorMessage(Command command, ErrorCodes error)
        {
            CreateErrorMessage(command, error).Send();
        }

        public MessageBuilder CreateErrorMessage(Command command, ErrorCodes error)
        {
            return Message.Builder.SetCommand(command).ToCharacter(this).WithError(error);
        }

        public void CheckPrivilegedTransactionsAndThrowIfFailed()
        {
            IsPrivilegedTransactionsAllowed().ThrowIfError();
        }

        public ErrorCodes IsPrivilegedTransactionsAllowed()
        {
            var accessLevel = AccessLevel;
            if (!accessLevel.IsAnyPrivilegeSet())
                return ErrorCodes.NoError;

            if (!accessLevel.IsAdminOrGm())
                return ErrorCodes.AccessDenied;

            return ErrorCodes.NoError;
        }

        public bool IsRobotSelectedForOtherCharacter(long robotEid)
        {
            var selectCheck = Db.Query().CommandText("select count(*) from characters where activechassis=@robotEID and characterID != @characterID")
                .SetParameter("@characterID", Id).SetParameter("@robotEID", robotEid)
                .ExecuteNonQuery();

            if (selectCheck <= 0)
                return false;

            Logger.Error($"An evil attempt to select a robot twice happened. characterID:{Id} robotEID:{robotEid}");
            return true;
        }

        public IWallet<double> GetWalletWithAccessCheck(bool useCorporationWallet,TransactionType transactionType,params CorporationRole[] roles)
        {
            var thisCharacter = this;
            return GetWallet(useCorporationWallet,transactionType,role =>
            {
                if (role.IsAnyRole(CorporationRole.CEO,CorporationRole.DeputyCEO,CorporationRole.Accountant))
                    return true;

                if (!role.IsAnyRole(CorporationRole.CEO))
                    thisCharacter._corporationManager.IsInJoinOrLeave(thisCharacter).ThrowIfError();

                return role.IsAnyRole(roles);
            });
        }

        public IWallet<double> GetWallet(bool useCorporationWallet,TransactionType transactionType,Predicate<CorporationRole> accessChecker = null)
        {
            if (!useCorporationWallet)
                return GetWallet(transactionType);

            var privateCorporation = GetPrivateCorporationOrThrow();

            if (accessChecker != null)
            {
                var role = privateCorporation.GetMemberRole(this);
                if (!accessChecker(role))
                    throw new PerpetuumException(ErrorCodes.InsufficientPrivileges);
            }

            return new CorporationWallet(privateCorporation);
        }

        public IWallet<double> GetWallet(TransactionType transactionType)
        {
            return _walletHelper.GetWallet(this,transactionType);
        }

        public void TransferCredit(Character target, long amount)
        {
            _walletHelper.TransferCredit(this,target,amount);
        }

        public void AddToWallet(TransactionType transactionType, double amount)
        {
            _walletHelper.AddToWallet(this,transactionType,amount);
        }

        public void SubtractFromWallet(TransactionType transactionType, double amount)
        {
            _walletHelper.SubtractFromWallet(this,transactionType,amount);
        }

        public ICharacterSocial GetSocial()
        {
            return _socialService.GetCharacterSocial(this);
        }

        public IEnumerable<Extension> GetDefaultExtensions()
        {
            return _extensionReader.GetCharacterDefaultExtensions(this);
        }

        public void ResetAllExtensions()
        {
            DeleteAllSpentPoints();

            //reset the actual extension levels
            Db.Query().CommandText("delete characterextensions where characterid=@characterID")
                    .SetParameter("@characterID",Id)
                    .ExecuteNonQuery();

            //reset remove log
            Db.Query().CommandText("delete extensionremovelog where characterid=@characterID")
                .SetParameter("@characterID",Id)
                .ExecuteNonQuery();

            //remove ram
            _characterExtensions.Remove(this);
        }

        public void IncreaseExtensionLevel(int extensionId, int extensionLevel)
        {
            Db.Query().CommandText("dbo.increaseExtensionLevel")
                .SetParameter("@characterID", Id)
                .SetParameter("@extensionID", extensionId)
                .SetParameter("@extensionLevel", extensionLevel)
                .ExecuteScalar<int>().ThrowIfEqual(0, ErrorCodes.SQLExecutionError);

            var thisCharacter = this;
            Transaction.Current.OnCommited(() => thisCharacter._characterExtensions.Remove(thisCharacter));
        }

        public int GetExtensionLevel(int extensionId)
        {
            return GetExtensions().GetLevel(extensionId);
        }

        public CharacterExtensionCollection GetExtensions()
        {
            return _characterExtensions.Get(this);
        }

        public void SetAllExtensionLevel(int level)
        {
            var extensions = _extensionReader.GetExtensions().Values.Where(e => !e.hidden).Select(e => new Extension(e.id, level));
            SetExtensions(extensions);
        }

        public void SetExtensions(IEnumerable<Extension> extensions)
        {
            foreach (var extension in extensions)
            {
                SetExtension(extension);
            }
        }

        public void SetExtension(Extension extension)
        {
            Logger.Info($"extid:{extension.id} level:{extension.level}");

            if (_extensionReader.GetExtensionByID(extension.id) == null)
            {
                Logger.Error($">>>> !!!!!!! >>>>>   extension not exists: {extension}");
                return;
            }

            Db.Query().CommandText("dbo.setExtensionLevel")
                .SetParameter("@characterID", Id)
                .SetParameter("@extensionID", extension.id)
                .SetParameter("@extensionLevel", extension.level)
                .ExecuteScalar<int>().ThrowIfEqual(0, ErrorCodes.SQLExecutionError);

            var tmpCharacter = this;
            Transaction.Current.OnCommited(() => tmpCharacter._characterExtensions.Remove(tmpCharacter));
        }

        public bool CheckLearnedExtension(Extension extension)
        {
            return GetExtensions().GetLevel(extension.id) >= extension.level;
        }

        public double GetExtensionsBonusSummary(params string[] extensionNames)
        {
            return GetExtensionsBonusSummary(_extensionReader.GetExtensionIDsByName(extensionNames));
        }

        public double GetExtensionsBonusSummary(IEnumerable<int> extensionIDs)
        {
            var thisC = this;
            return GetExtensions().SelectById(extensionIDs).Sum(e => e.level * thisC._extensionReader.GetExtensionByID(e.id).bonus);
        }

        public double GetExtensionBonusByName(string extensionName)
        {
            return GetExtensionBonus(_extensionReader.GetExtensionIDByName(extensionName));
        }

        public double GetExtensionBonus(int extensionId)
        {
            return GetExtensions().GetLevel(extensionId) * _extensionReader.GetExtensionByID(extensionId).bonus;
        }

        public int GetExtensionLevelSummaryByName(params string[] extensionNames)
        {
            var ex = GetExtensions();
            return _extensionReader.GetExtensionIDsByName(extensionNames).Sum(extensionId => ex.GetLevel(extensionId));
        }

        public double GetExtensionBonusWithPrerequiredExtensions(string extensionName)
        {
            return GetExtensionBonusWithPrerequiredExtensions(_extensionReader.GetExtensionIDByName(extensionName));
        }

        public double GetExtensionBonusWithPrerequiredExtensions(int extensionId)
        {
            return GetExtensionsBonusSummary(_extensionReader.GetExtensionPrerequireTree(extensionId).Distinct());
        }

        public bool IsFriend(Character otherCharacter)
        {
            return GetSocial().GetFriendSocialState(otherCharacter) == SocialState.Friend;
        }

        public bool IsBlocked(Character otherCharacter)
        {
            return GetSocial().GetFriendSocialState(otherCharacter) == SocialState.Blocked;
        }

        [Pure]
        public int AddExtensionPointsBoostAndLog(EpForActivityType activityType,   int points)
        {
            if (points <= 0)
                return 0;

            var account = GetAccount();
            Debug.Assert(account != null, "account != null");
            return _accountManager.AddExtensionPointsBoostAndLog(account,this, activityType, points);
        }


        [CanBeNull]
        public Task ReloadContainerOnZoneAsync()
        {
            var character = this;
            return Task.Run(() => character.ReloadContainerOnZone());
        }

        public void ReloadContainerOnZone()
        {
            GetPlayerRobotFromZone()?.ReloadContainer();
        }

        public PublicContainer GetPublicContainer()
        {
            return GetCurrentDockingBase().GetPublicContainer();
        }

        public PublicContainer GetPublicContainerWithItems()
        {
            return GetCurrentDockingBase().GetPublicContainerWithItems(this);
        }

        public void SendItemErrorMessage(Command command,ErrorCodes error,Item item)
        {
            CreateErrorMessage(command,error)
                .SetExtraInfo(d =>
                {
                    d["eid"] = item.Eid;
                    d["name"] = item.ED.Name;
                }).Send();
        }

        public bool TechTreeNodeUnlocked(int definition)
        {
            if (_techTreeService.GetUnlockedNodes(Eid).Any(n => n.Definition == definition))
                return true;

            var hasRole = Corporation.GetRoleFromSql(this).HasRole(PresetCorporationRoles.CAN_LIST_TECHTREE);
            return hasRole && _techTreeService.GetUnlockedNodes(CorporationEid).Any(n => n.Definition == definition);
        }

        public bool HasTechTreeBonus(int definition)
        {
            var hasRole = Corporation.GetRoleFromSql(this).HasRole(PresetCorporationRoles.CAN_LIST_TECHTREE);
            if (!hasRole)
                return false;

            return _techTreeService.GetUnlockedNodes(Eid).Any(n => n.Definition == definition) && (_techTreeService.GetUnlockedNodes(CorporationEid).Any(n => n.Definition == definition));
        }

        public DockingBase GetHomeBaseOrCurrentBase()
        {
            var resultBaseEid = HomeBaseEid ?? 0L;
            var wasHomeBase = true;

            if (resultBaseEid == 0)
            {
                resultBaseEid = CurrentDockingBaseEid;
                wasHomeBase = false;
            }

            var dockingBase = _dockingBaseHelper.GetDockingBase(resultBaseEid);
            if (dockingBase != null && dockingBase.IsDockingAllowed(this) == ErrorCodes.NoError)
                return _dockingBaseHelper.GetDockingBase(resultBaseEid);

            //docking would normally fail
            if (wasHomeBase)
            {
                //was homebase set, clear it
                HomeBaseEid = null;
            }

            //pick the race related homebase
            resultBaseEid = DefaultCorporation.GetDockingBaseEid(this);

            var thisCharacter = this;
            //inform dead player about this state
            Transaction.Current.OnCommited(() =>
            {
                var info = new Dictionary<string,object>
                {
                    {k.characterID,thisCharacter.Id},
                    {k.baseEID, resultBaseEid},
                    {k.wasDeleted, wasHomeBase}
                };

                Message.Builder.SetCommand(Commands.CharacterForcedToBase).WithData(info).ToCharacter(thisCharacter).Send();
            });

            return _dockingBaseHelper.GetDockingBase(resultBaseEid);
        }

        public void WriteItemTransactionLog(TransactionType transactionType,Item item)
        {
            var b = TransactionLogEvent.Builder().SetTransactionType(transactionType).SetCharacter(this).SetItem(item.Definition,item.Quantity);
            LogTransaction(b);
        }

        public void SetActiveRobot(Robot robot)
        {
            ActiveRobotEid = robot?.Eid ?? 0L;

            var thisCharacter = this;
            var result = robot?.ToDictionary();
            Transaction.Current.OnCommited(() => Message.Builder.SetCommand(Commands.RobotActivated)
                .WithData(result)
                .WrapToResult()
                .ToCharacter(thisCharacter)
                .Send());
        }

        public void CleanGameRelatedData()
        {
            //corp founder
            Db.Query().CommandText("update corporations set founder=NULL where founder=@characterId")
                .SetParameter("@characterID",Id)
                .ExecuteNonQuery();

            Db.Query().CommandText("delete characterextensions where characterid=@characterID")
                .SetParameter("@characterID",Id)
                .ExecuteNonQuery();

            Db.Query().CommandText("delete charactersettings where characterid=@characterID")
                .SetParameter("@characterID",Id)
                .ExecuteNonQuery();

            Db.Query().CommandText("delete charactersparks where characterid=@characterID")
                .SetParameter("@characterID",Id)
                .ExecuteNonQuery();

            Db.Query().CommandText("delete charactersparkteleports where characterid=@characterID")
                .SetParameter("@characterID",Id)
                .ExecuteNonQuery();

            Db.Query().CommandText("delete from channelmembers where memberid = @characterID")
                .SetParameter("@characterID",Id)
                .ExecuteNonQuery();

            TransportAssignment.CharacterDeleted(this);
        }

        [CanBeNull]
        public Robot GetActiveRobot()
        {
            return _robotHelper.LoadRobotForCharacter(ActiveRobotEid, this, true);
        }

        [CanBeNull]
        public Gang GetGang()
        {
            return _gangManager.GetGangByMember(this);
        }

        [CanBeNull]
        public Alliance GetAlliance()
        {
            var allianceEid = AllianceEid;
            return allianceEid == 0L ? null : Alliance.GetOrThrow(allianceEid);
        }

        public bool IsRobotSelectedForCharacter(Robot robot)
        {
            return ActiveRobotEid == robot?.Eid;
        }

        public DockingBase GetCurrentDockingBase()
        {
            var baseEid = CurrentDockingBaseEid;
            return _dockingBaseHelper.GetDockingBase(baseEid);
        }

        [CanBeNull]
        public Player GetPlayerRobotFromZone()
        {
            var zone = GetCurrentZone();
            return zone?.GetPlayer(this);
        }

        public IZone GetCurrentZone()
        {
            return _zoneManager.Value.GetZone(ZoneId ?? -1);
        }

        public IZone GetZone(int zoneiwant)
        {
            return _zoneManager.Value.GetZone(zoneiwant);
        }

        public ZoneConfiguration GetCurrentZoneConfiguration()
        {
            return GetCurrentZone()?.Configuration ?? ZoneConfiguration.None;
        }

        public IDictionary<string,object> GetFullProfile()
        {
            var record = Db.Query().CommandText("select * from characters where characterid = @characterid")
                .SetParameter("@characterid",Id)
                .ExecuteSingleRow().ThrowIfNull(ErrorCodes.CharacterNotFound);

            var currentBaseEid = record.GetValue<long>("baseEID");
            var dockingBase =  _dockingBaseHelper.GetDockingBase(currentBaseEid);
            var isInTraining = record.GetValue<int>("raceID") == 0;

            var profile = new Dictionary<string,object>(21)
            {
                {k.raceID, record.GetValue<int>("raceID")},
                {k.creation, record.GetValue<DateTime>("creation")},
                {k.nick, record.GetValue<string>("nick")},
                {k.moodMessage, record.GetValue<string>("moodMessage")},
                {k.credit, (long) record.GetValue<double>("credit")},
                {k.lastUsed, record.IsDBNull("lastused") ? (object) null : record.GetValue<DateTime>("lastused")},
                {k.rootEID, record.GetValue<long>("rootEID")},
                {k.totalMinsOnline, record.GetValue<int>("totalMinsOnline")},
                {k.activeChassis, record.IsDBNull("activeChassis") ? (object) null : record.GetValue<long>("activeChassis")},
                {k.baseEID, record.IsDBNull("baseEID") ? (object) null : record.GetValue<long>("baseEID")},
                {k.majorID, record.GetValue<int>("majorID")},
                {k.schoolID, record.GetValue<int>("schoolID")},
                {k.sparkID, record.GetValue<int>("sparkID")},
                {k.defaultCorporation, record.GetValue<long>("defaultcorporationEID")},
                {k.corporationEID, CorporationEid},
                {k.allianceEID, AllianceEid},
                {k.avatar, (GenxyString) record.GetValue<string>("avatar")},
                {k.lastLogOut, record.IsDBNull("lastLogOut") ? (object) null : record.GetValue<DateTime>("lastLogOut")},
                {k.language, record.GetValue<int>("language")},
                {k.zoneID, record.GetValue<int?>("zoneID")},
                {k.homeBaseEID, record.GetValue<long?>("homeBaseEID")},
                {k.blockTrades, record.GetValue<bool>("blockTrades")},
                {k.dockingBaseInfo, dockingBase?.GetDockingBaseDetails()},
                {k.isInTraining, isInTraining}
            };

            _techTreeService.AddInfoToDictionary(Eid,profile);
            return profile;
        }

        public PrivateCorporation GetPrivateCorporationOrThrow()
        {
            return GetPrivateCorporation().ThrowIfNull(ErrorCodes.CharacterMustBeInPrivateCorporation);
        }

        [CanBeNull]
        public PrivateCorporation GetPrivateCorporation()
        {
            return (GetCorporation() as PrivateCorporation);
        }

        public Corporation GetCorporation()
        {
            return Corporation.GetOrThrow(CorporationEid);
        }

        public DefaultCorporation GetDefaultCorporation()
        {
            var eid = DefaultCorporationEid;
            return (DefaultCorporation)Corporation.GetOrThrow(eid);
        }

        public void DeleteAllSpentPoints()
        {
            //delete the spent points
            Db.Query().CommandText("delete accountextensionspent where characterid=@characterID")
                .SetParameter("@characterID", Id)
                .ExecuteNonQuery();
        }

        public void GetTableIndexForAccountExtensionSpent(int extensionId, int extensionLevel, ref int spentId, ref int spentPoints)
        {
            var record = Db.Query().CommandText("select top 1 id,points from accountextensionspent where extensionid=@extensionID and extensionlevel=@extensionLevel and characterid=@characterID and points > 0 order by eventtime desc")
                .SetParameter("@extensionLevel", extensionLevel)
                .SetParameter("@extensionID", extensionId)
                .SetParameter("@characterID", Id)
                .ExecuteSingleRow();

            if (record == null)
                return;

            spentId = record.GetValue<int>(0);
            spentPoints = record.GetValue<int>(1);

            Debug.Assert(spentPoints > 0, "extension fuckup error");
        }
    }
}
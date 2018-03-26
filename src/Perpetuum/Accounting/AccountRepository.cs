using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Perpetuum.Data;

namespace Perpetuum.Accounting
{
    public class AccountRepository : IAccountRepository
    {
        public void Insert(Account account)
        {
            account.Id = Db.Query(@"insert accounts 
                                             (steamid,email,password,accLevel,emailconfirmed,isactive,campaignid) values 
                                             (@steamId,@email,@password,@accessLevel,@emailConfirmed,@isActive,@campaignid);select cast(scope_identity() as int)")
                .SetParameter("steamId", account.SteamId)
                .SetParameter("email",account.Email)
                .SetParameter("password",account.Password)
                .SetParameter("accessLevel",(int)account.AccessLevel)
                .SetParameter("emailConfirmed", account.EmailConfirmed)
                .SetParameter("isActive", account.IsActive)
                .SetParameter("campaignid",account.CampaignId)
                .ExecuteScalar<int>().ThrowIfEqual(0,ErrorCodes.SQLInsertError);
        }

        public void Update(Account account)
        {
            var q = Db.Query(@"update accounts set 
                                                 email = @email,
                                                 password = @password,
                                                 accLevel = @accessLevel,
                                                 state = @state,
                                                 validuntil = @validUntil,
                                                 payingcustomer = @payingcustomer,
                                                 firstcharacter = @firstcharacter,
                                                 isloggedin = @isloggedin,
                                                 lastloggedin = @lastloggedin,
                                                 totalminsonline = @totalminsonline,
                                                 credit = @credit,
                                                 bantime = @bantime,
                                                 banlength = @banlength,
                                                 bannote = @bannote
                                                 where accountid = @id")
                .SetParameter("email",account.Email)
                .SetParameter("password",account.Password)
                .SetParameter("accessLevel",(int)account.AccessLevel)
                .SetParameter("state", account.State)
                .SetParameter("validUntil",account.ValidUntil)
                .SetParameter("payingcustomer", account.PayingCustomer)
                .SetParameter("firstcharacter",account.FirstCharacterDate)
                .SetParameter("isloggedin",account.IsLoggedIn)
                .SetParameter("lastloggedin",account.LastLoggedIn)
                .SetParameter("totalminsonline",(int)account.TotalOnlineTime.TotalMinutes)
                .SetParameter("credit",account.Credit)
                .SetParameter("bantime", account.BanTime)
                .SetParameter("banlength", (int)account.BanLength.TotalSeconds)
                .SetParameter("bannote", account.BanNote)
                .SetParameter("id", account.Id)
                .ExecuteNonQuery().ThrowIfEqual(0,ErrorCodes.SQLUpdateError);
        }

        public AccessLevel GetAccessLevel(int accountId)
        {
            return (AccessLevel) Db.Query("select accLevel from accounts where accountid = @id")
                                  .SetParameter("id", accountId)
                                  .ExecuteScalar<int>();
        }

        private Account CreateAccountFromRecord(IDataRecord record)
        {
            if (record == null)
                return null;

            var account = new Account
            {
                Id = record.GetValue<int>("accountid"),
                Password = record.GetValue<string>("password"),
                SteamId = record.GetValue<string>("steamId"),
                Creation = record.GetValue<DateTime>("creation"),
                AccessLevel = (AccessLevel) record.GetValue<int>("accLevel"),
                EmailConfirmed = record.GetValue<bool>("emailConfirmed"),
                Email = record.GetValue<string>("email"),
                BanTime = record.GetValue<DateTime?>("banTime"),
                BanLength = TimeSpan.FromSeconds(record.GetValue<int>("banLength")),
                BanNote = record.GetValue<string>("banNote"),
                TwitchAuthToken = record.GetValue<string>("twitchAuthToken"),
                State = (AccountState) record.GetValue<int>("state"),
                ValidUntil = record.GetValue<DateTime?>("validuntil"),
                PayingCustomer = record.GetValue<bool>("payingcustomer"),
                IsActive = record.GetValue<bool>("isactive"),
                FirstCharacterDate = record.GetValue<DateTime?>("firstcharacter"),
                IsLoggedIn = record.GetValue<bool>("isloggedin"),
                LastLoggedIn = record.GetValue<DateTime?>("lastloggedin"),
                TotalOnlineTime = TimeSpan.FromMinutes(record.GetValue<int>("totalminsonline")),
                Credit = record.GetValue<int>("credit")
            };
            return account;
        }

        public Account Get(int accountId)
        {
            var record = Db.Query("select * from accounts where accountId = @accountId")
                .SetParameter("accountId", accountId)
                .ExecuteSingleRow();

            return CreateAccountFromRecord(record);
        }

        public Account Get(int accountId, string steamId)
        {
            var record = Db.Query("select * from accounts where steamId = @steamId and accountId = @accountId")
                                .SetParameter("accountId", accountId)
                                .SetParameter("steamId", steamId)
                                .ExecuteSingleRow();

            return CreateAccountFromRecord(record);
        }

        public Account Get(string email)
        {
            var record = Db.Query("select * from accounts where email = @email")
                                .SetParameter("email", email)
                                .ExecuteSingleRow();

            return CreateAccountFromRecord(record);
        }

        public Account Get(string email, string password)
        {
            var record = Db.Query("select * from accounts where email = @email and password = @password")
                                .SetParameter("email",email)
                                .SetParameter("password",password)
                                .ExecuteSingleRow();

            return CreateAccountFromRecord(record);
        }

        public IEnumerable<Account> GetBySteamId(string steamId)
        {
            return Db.Query("select * from accounts where steamId = @steamId")
                .SetParameter("steamId",steamId)
                .Execute().Select(CreateAccountFromRecord).ToArray();
        }

        public IEnumerable<Account> GetAll()
        {
            return Db.Query("select * from accounts").Execute().Select(CreateAccountFromRecord).ToArray();
        }

        public void Delete(Account item)
        {
            var result = Db.Query("saAccountDelete").SetParameter("accountid", item.Id).ExecuteScalar<int>();
            if (result != item.Id)
                throw new PerpetuumException(ErrorCodes.AccountNotFound);
        }
    }
}
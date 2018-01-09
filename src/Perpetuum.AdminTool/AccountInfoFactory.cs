using System;
using System.Collections.Generic;
using System.Linq;
using Perpetuum.Accounting;

namespace Perpetuum.AdminTool
{
    public class AccountInfoFactory : IAccountInfoFactory
    {
        public AccountInfo Create(Dictionary<string,object> accountData,IEnumerable<CharacterInfo> characters)
        {
            var accountInfo = new AccountInfo
            {
                Id = accountData.GetOrDefault<int>(k.accountID),
                Email = accountData.GetOrDefault<string>(k.email),
                AccessLevel = AccountInfo.ExtractAccessLevel(accountData),
                Password = AccountInfo.DEFAULT_PASSWORD_VALUE,
                AccountState = (AccountState) accountData.GetOrDefault<int>(k.accountState),
                BanTime = accountData.GetOrDefault<DateTime>(k.banTime),
                BanLength = TimeSpan.FromSeconds(accountData.GetOrDefault<int>(k.banLength)),
                BanNote = accountData.GetOrDefault<string>(k.banNote),
                Characters = characters?.ToList() ?? new List<CharacterInfo>()
            };
            accountInfo.IsModified = false;
            return accountInfo;
        }

        public AccountInfo CreateEmpty()
        {
            return new AccountInfo();
        }
    }
}
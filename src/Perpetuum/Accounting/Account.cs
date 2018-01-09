using System;
using System.Collections.Generic;

namespace Perpetuum.Accounting
{
    public class Account
    {
        public int Id { get; set; }
        public string SteamId { get; set; }
        public DateTime Creation { get; set; }
        public AccessLevel AccessLevel { get; set; }
        public string Email { get; set; }
        public bool EmailConfirmed { get; set; }
        public DateTime? BanTime { get; set; }
        public TimeSpan BanLength { get; set; }
        public string BanNote { get; set; }
        public string TwitchAuthToken { private get; set; }
        public AccountState State { get; set; }
        public DateTime? ValidUntil { get; set; }
        public string CampaignId { get; set; } = "{\"host\":\"steam\"}";
        public bool IsActive { get; set; }
        public DateTime? FirstCharacterDate { get; set; }
        public bool IsLoggedIn { get; set; }
        public DateTime? LastLoggedIn { get; set; }
        public TimeSpan TotalOnlineTime { get; set; }
        public int Credit { get; set; }
        public string Password { get; set; }
        public bool PayingCustomer { get; set; }

        public bool IsDailyEpBoosted => ValidUntil > DateTime.Now;

        public IDictionary<string, object> ToDictionary()
        {
            var dictionary = new Dictionary<string, object>
            {
                    {k.accountID, Id},
                    {k.accLevel, (int) AccessLevel},
                    {k.accountState, (int) State},
                    {k.isSubscriber, ValidUntil != DateTime.MaxValue},
                    {k.isEarlyAccess,false},
                    {k.validUntil, ValidUntil},
                    {k.email,Email},
                    {k.credit,Credit},
                    {"twitchAuthToken",TwitchAuthToken},
                    {k.banLength, (int)BanLength.TotalSeconds },
                    {k.banNote , BanNote },
                    {k.banTime, BanTime }
                };

            return dictionary;
        }
    }
}
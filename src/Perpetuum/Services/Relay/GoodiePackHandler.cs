using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using Perpetuum.Accounting;
using Perpetuum.Accounting.Characters;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.Items;

namespace Perpetuum.Services.Relay
{
    public class GoodiePackHandler
    {
        private readonly IAccountManager _accountManager;

        public GoodiePackHandler(IAccountManager accountManager)
        {
            _accountManager = accountManager;
        }

        private IEnumerable<GoodiePack> GetGoodiePacks(Account account)
        {
            var campaignIds = GetNonRedeemedCampaignIds(account).ToArray();

            if (campaignIds.IsNullOrEmpty()) 
                return Enumerable.Empty<GoodiePack>();

            var campaignStr = campaignIds.ArrayToString();
            return Db.Query()
                     .CommandText("select * from campaigngoodiepacks where campaignid in (" + campaignStr + ")")
                     .Execute()
                     .Select(record =>
                {

                    var goodiePack = new GoodiePack
                    {
                        Id = record.GetValue<int>("id"),
                        Name = record.GetValue<string>("name"),
                        Description = record.GetValue<string>("description"),
                        Credit = record.GetValue<int?>("credit"),
                        Ep = record.GetValue<int?>("ep"),
                        CampaignId = record.GetValue<int>("campaignid"),
                        Faction = record.GetValue<string>("faction")
                    };

                    var items = new List<ItemInfo>();
                    for (var i = 0;i < GoodiePack.ITEMS_COUNT;i++)
                    {
                        var itemInfo = ItemInfo.None;
                        var definition = record.GetValue<int?>("item" + i);
                        if (definition != null)
                        {
                            var qty = record.GetValue<int?>("quantity" + i) ?? 1;
                            itemInfo = new ItemInfo((int)definition,qty)
                            {
                                IsRepackaged = true
                            };
                        }

                        items.Add(itemInfo);
                    }

                    goodiePack.Items = items;

                    return goodiePack;

                }).ToArray();
        }

        private IEnumerable<int> GetNonRedeemedCampaignIds(Account account)
        {
            return Db.Query().CommandText("select campaignid from accountcampaignitems where redeemed=0 and accountid=@accountID")
                           .SetParameter("@accountID", account.Id)
                           .Execute()
                           .Select(r => r.GetValue<int>(0)).ToArray();
        }

        public Dictionary<string, object> ListGoodiePacks(Account account)
        {
            var packs = new Dictionary<string, object>();
            var counter = 0;
            foreach (var p in GetGoodiePacks(account))
            {
                var onePack = p.ToDictionary();
                packs.Add("p" + counter++, onePack);
            }

            var campaigns = new Dictionary<string, object>();

            var records = Db.Query().CommandText("select id,campaigntoken from campaigns").Execute();

            foreach (var r in records)
            {
                var oneEntry = new Dictionary<string, object>
                    {
                        {k.ID, r.GetValue<int>(0)},
                        {k.name, r.GetValue<string>(1)}
                    };

                campaigns.Add("c"+counter++, oneEntry);
            }
            
            var result = new Dictionary<string, object>
                {
                    {"goodiePacks", packs},
                    {"campaigns", campaigns}
                };

            return result;
        }

        public IDictionary<string,object> RedeemPackBySelection(int campaignId,Account account,Character character, bool isPackIndy)
        {
            var packs = GetGoodiePacks(account).ToArray();
            packs.IsNullOrEmpty().ThrowIfTrue(ErrorCodes.ItemNotFound);

            GoodiePack goodiePack;

            if (isPackIndy)
            {
                goodiePack = packs.FirstOrDefault(p => p.Faction == "indy" && p.CampaignId == campaignId);
            }
            else
            {
                //race alapjan select
                var defaultCorporation = character.GetDefaultCorporation();
                var nick = defaultCorporation.Description.nick.ToLower();

                goodiePack = packs.FirstOrDefault(p => nick.Contains(p.Faction.ToLower()) && p.CampaignId == campaignId);
            }

            goodiePack.ThrowIfNull(ErrorCodes.ServerError);
            Debug.Assert(goodiePack != null, "goodiePack != null");
            return Redeem(account, character,goodiePack);
        }

        public IDictionary<string, object> GetMyRedeemableItems(Account account)
        {
            var result = RedeemableItemInfo.GetAll(account).ToDictionary("r", i => i.ToDictionary());
            return new Dictionary<string, object>
                {
                    {"redeemables", result}
                };
        }

        public IDictionary<string,object> Redeem(Account account,Character character,GoodiePack goodiePack)
        {
            var result = new Dictionary<string,object>();

            if (goodiePack.Credit != null)
            {
                character.AddToWallet(TransactionType.GoodiePackCredit,(double)goodiePack.Credit);
            }

            if (goodiePack.Ep != null)
            {
                //ExtensionHelper.AddExtensionPenaltyPoints(account, -1*(int) _ep, true);

                var epData = _accountManager.GetEPData(account,character);
                result.Add(k.extension,epData);
            }

            var publicContainer = character.GetPublicContainerWithItems();

            foreach (var itemInfo in goodiePack.Items.Where(itemInfo => itemInfo != ItemInfo.None))
            {
                publicContainer.CreateAndAddItem(itemInfo,i =>
                {
                    i.Owner = character.Eid;
                });
            }

            publicContainer.Save();
            RedeemDone(account,goodiePack);

            var items = publicContainer.ToDictionary();
            result.Add(k.container,items);
            result.Add("goodiepack",goodiePack.ToDictionary());
            return result;
        }

        private void RedeemDone(Account account,GoodiePack goodiePack)
        {
            Db.Query().CommandText("update accountcampaignitems set redeemed=1,redeemdate=@now where accountid=@accountID and campaignid=@campaignID")
                .SetParameter("@now",DateTime.Now)
                .SetParameter("@accountID",account.Id)
                .SetParameter("@campaignID",goodiePack.CampaignId)
                .ExecuteNonQuery().ThrowIfEqual(0,ErrorCodes.SQLUpdateError);
        }
    }

    public class RedeemableItemInfo
    {
        private readonly int _id;
        private readonly int _definition;
        private readonly int _quantity;

        private RedeemableItemInfo(int id,int definition,int quantity)
        {
            _id = id;
            _definition = definition;
            _quantity = quantity;
        }

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
                {
                    {k.ID, _id},
                    {k.definition, _definition},
                    {k.quantity, _quantity}
                };
        }

        public List<Item> CreateItems()
        {
            var ed = EntityDefault.Get(_definition);

            var result = new List<Item>();

            if (!ed.AttributeFlags.NonStackable)
            {
                var item = (Item) Entity.Factory.Create(_definition, EntityIDGenerator.Random);
                item.Quantity = _quantity;
                result.Add(item);
            }
            else
            {
                for (var i = 0; i < _quantity; i++)
                {
                    var item = (Item)Entity.Factory.Create(_definition, EntityIDGenerator.Random);
                    item.Quantity = 1;
                    result.Add(item);
                }
            }

            return result;
        }

        [NotNull]
        public static RedeemableItemInfo LoadRedeemableItemById(int id,Account account)
        {
            const string selectCommandText = "select id,definition,quantity from accountredeemableitems where id=@id and accountid=@accountId and wasredeemed = 0";
            var record = Db.Query().CommandText(selectCommandText)
                .SetParameter("@id",id)
                .SetParameter("@accountId",account.Id)
                .ExecuteSingleRow().ThrowIfNull(ErrorCodes.ItemNotFound);

            return CreateFromRecord(record);
        }

        public void SetRedeemed(Character character)
        {
            Db.Query().CommandText("update accountredeemableitems set wasredeemed=1,characterid=@characterID,redeemed=@now where id=@id")
                .SetParameter("@id", _id)
                .SetParameter("@characterID", character.Id)
                .SetParameter("@now", DateTime.Now)
                .ExecuteNonQuery().ThrowIfEqual(0,ErrorCodes.SQLUpdateError);
        }

        public static List<RedeemableItemInfo> GetAll(Account account)
        {
            return Db.Query().CommandText("select id,definition,quantity from accountredeemableitems where accountid=@accountId and wasredeemed=0")
                          .SetParameter("@accountId", account.Id).Execute()
                          .Select(CreateFromRecord).ToList();
        }

        private static RedeemableItemInfo CreateFromRecord(IDataRecord record)
        {
            var id = record.GetValue<int>("id");
            var definition = record.GetValue<int>("definition");
            var quantity = record.GetValue<int>("quantity");
            return new RedeemableItemInfo(id, definition, quantity);
        }
    }

    public abstract class RedeemableItem : Item
    {
        protected IAccountManager AccountManager { get; private set; }

        protected RedeemableItem(IAccountManager accountManager)
        {
            this.AccountManager = accountManager;
        }

        public abstract void Activate(Account account,Character character);
    }
}

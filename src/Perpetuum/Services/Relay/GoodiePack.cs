using System.Collections.Generic;
using Perpetuum.Items;

namespace Perpetuum.Services.Relay
{
    public class GoodiePack
    {
        public const int ITEMS_COUNT = 10;
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Faction { get; set; }
        public int CampaignId { get; set; }

        public int? Credit { get; set; }
        public int? Ep { get; set; }

        public List<ItemInfo> Items { get; set; } = new List<ItemInfo>(ITEMS_COUNT);

        public Dictionary<string,object> ToDictionary()
        {
            var dictionary = new Dictionary<string, object>
            {
                {k.ID, Id},
                {k.name, Name},
                {k.description, Description},
                {"campaignid", CampaignId},
                {k.credit, Credit},
                {k.extensionPoints, Ep},
            };

            for (var i = 0; i < Items.Count; i++)
            {
                int? definition = null;
                int? quantity = null;

                var itemInfo = Items[i];
                if (itemInfo != ItemInfo.None)
                {
                    definition = itemInfo.Definition;
                    quantity = itemInfo.Quantity;
                }

                dictionary["item" + i] = definition;
                dictionary["quantity" + i] = quantity;
            }

            return dictionary;
        }
    }
}
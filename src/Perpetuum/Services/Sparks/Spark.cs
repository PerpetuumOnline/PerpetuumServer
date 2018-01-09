using System.Collections.Generic;
using Perpetuum.Accounting.Characters;

namespace Perpetuum.Services.Sparks
{
    public class Spark
    {
        public int id;
        public string sparkName;
        public int? unlockPrice;
        public int? energyCredit;
        public double? standingLimit;
        public long? allianceEid;
        public int? definition;
        public int? quantity;
        public int changePrice;
        public int displayOrder;
        public bool defaultSpark;
        public string icon;
        public bool hidden;
        public bool unlockable;
        public List<SparkExtension> Extensions { get; set; }
        public List<Extension> RelatedExtensions { get; set; }

        public Dictionary<string,object> ToDictionary()
        {
            var info = new Dictionary<string, object>
            {
                {k.ID, id},
                {k.name, sparkName},
                {k.unlockPrice, unlockPrice},
                {k.energyCredit, energyCredit},
                {k.standing, standingLimit},
                {k.allianceEID, allianceEid},
                {k.definition, definition},
                {k.quantity, quantity},
                {k.changePrice, changePrice},
                {k.displayOrder, displayOrder},
                {k.defaultSpark, defaultSpark},
                {k.icon, icon},
                {"unlockable", unlockable},
                {k.sparkExtensions, Extensions.ToDictionary("e", se => se.ToDictionary())}
            };

            return info;
        }

        public void DeleteRelatedExtensions(Character character)
        {
            foreach (var extension in RelatedExtensions)
            {
                character.SetExtension(new Extension(extension.id, 0));
            }
        }
            
        public void SetRelatedExtensions(Character character)
        {
            foreach (var extension in RelatedExtensions)
            {
                character.SetExtension(extension);
            }
        }
    }
}

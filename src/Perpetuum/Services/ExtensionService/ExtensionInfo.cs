using System.Collections.Generic;
using System.Data;
using Perpetuum.Data;
using Perpetuum.ExportedTypes;

namespace Perpetuum.Services.ExtensionService
{
    public class ExtensionInfo
    {
        public int id;
        public string name;
        public int category;
        public int rank;
        public string learningAttributePrimary;
        public string learningAttributeSecondary;
        public double bonus;
        public int price;
        private string _description;
        public AggregateField aggregateField;
        public bool hidden;
        public int? freezeLimit;

        public Extension[] RequiredExtensions { get; set; }

        public ExtensionInfo(IDataRecord record)
        {
            id = record.GetValue<int>("extensionid");
            name = record.GetValue<string>("extensionname");
            category = record.GetValue<int>("category");
            rank = record.GetValue<int>("rank");
            learningAttributePrimary = record.GetValue<string>("learningattributeprimary");
            learningAttributeSecondary = record.GetValue<string>("learningattributesecondary");
            bonus = record.GetValue<double>("bonus");
            price = record.GetValue<int>("price");
            _description = record.GetValue<string>("description");
            aggregateField = (AggregateField)(record.GetValue<int?>("targetpropertyID") ?? 0);
            hidden = record.GetValue<bool>("hidden");
            freezeLimit = record.GetValue<int?>("freezelimit");
        }

        public override string ToString()
        {
            return $"name:{name} id:{id}";
        }

        public Dictionary<string,object> ToDictionary()
        {
            return new Dictionary<string, object>
                {
                    {k.extensionID, id},
                    {k.name, name},
                    {k.category, category},
                    {k.rank, rank},
                    {k.price, price},
                    {k.bonus, bonus},
                    {k.learningAttributePrimary, learningAttributePrimary},
                    {k.learningAttributeSecondary, learningAttributeSecondary},
                    {k.description, _description},
                    {k.hidden, hidden},
                    {k.freezeLimit, freezeLimit},
                };
        }
    }
}
using System.Collections.Generic;
using Perpetuum.EntityFramework;
using Perpetuum.Robots;

namespace Perpetuum.Items.Templates
{
    public delegate RobotTemplate RobotTemplateFactory(int definition);


    public class RobotTemplate : ItemTemplate<Robot>
    {
        public int ID { get; set; }
        public string Name { get; set; }

        public RobotComponentTemplate<RobotHead> Head { get; set; }
        public RobotComponentTemplate<RobotChassis> Chassis { get; set; }
        public RobotComponentTemplate<RobotLeg> Leg { get; set; }
        public RobotInventoryTemplate Inventory { get; set; }

        public RobotTemplate(string name) : base(1, false)
        {
            Name = name;
        }

        public override Dictionary<string, object> ToDictionary()
        {
            var dictionary = new Dictionary<string, object>
            {
                {k.robot,EntityDefault.Definition},
                {k.head, Head.EntityDefault.Definition},
                {k.chassis, Chassis.EntityDefault.Definition},
                {k.leg, Leg.EntityDefault.Definition},
                {k.container,Inventory.EntityDefault.Definition},
                {k.headModules,Head.Modules.ToDictionary("m", m => m.ToDictionary())},
                {k.chassisModules,Chassis.Modules.ToDictionary("m", m => m.ToDictionary())},
                {k.legModules,Leg.Modules.ToDictionary("m", m => m.ToDictionary())},
                {k.items,Inventory.Items.ToDictionary("i", i => i.ToDictionary())}
            };

            return dictionary;
        }

        [CanBeNull]
        public static RobotTemplate CreateFromDictionary(string name,IDictionary<string, object> dictionary)
        {
            if (dictionary == null)
                return null;

            var template = new RobotTemplate(name)
            {
                EntityDefault = EntityDefault.Get(dictionary.GetOrDefault(k.robot,0)),
                Head = RobotComponentTemplate<RobotHead>.Create(dictionary.GetOrDefault(k.head, 0), ModulesFromDictionary(dictionary,k.headModules)),
                Chassis = RobotComponentTemplate<RobotChassis>.Create(dictionary.GetOrDefault(k.chassis, 0), ModulesFromDictionary(dictionary,k.chassisModules)),
                Leg = RobotComponentTemplate<RobotLeg>.Create(dictionary.GetOrDefault(k.leg, 0), ModulesFromDictionary(dictionary,k.legModules)),
                Inventory = RobotInventoryTemplate.Create(dictionary.GetOrDefault(k.container, 0), ItemsFromDictionary(dictionary,k.items))
            };

            return template;
        }

        private static ModuleTemplate[] ModulesFromDictionary(IDictionary<string,object> dictinary,string key)
        {
            var md = dictinary.GetOrDefault<IDictionary<string,object>>(key);
            if ( md == null )
                return new ModuleTemplate[0];

            var templates = new List<ModuleTemplate>();

            foreach (var o in md.Values)
            {
                var t = ModuleTemplate.CreateFromDictionary((IDictionary<string, object>) o);
                templates.Add(t);
            }

            return templates.ToArray();
        }

        private static ItemTemplate<Item>[] ItemsFromDictionary(IDictionary<string,object> dd,string key)
        {
            var id = dd.GetOrDefault<IDictionary<string,object>>(key);
            if ( id == null )
                return new ItemTemplate<Item>[0];

            var templates = new List<ItemTemplate<Item>>();

            foreach (var o in id.Values)
            {
                var dictionary = (IDictionary<string, object>) o;
                var definition = dictionary.GetOrDefault(k.definition, 0);
                var quantity = dictionary.GetValue<int>(k.quantity);
                var repackaged = dictionary.GetOrDefault(k.repackaged, 0) > 0;

                var t = ItemTemplate<Item>.Create(definition, quantity, repackaged);
                templates.Add(t);
            }

            return templates.ToArray();
        }

        protected override bool OnValidate(Robot robot)
        {
            if (!Head.Validate())
                return false;

            if (!Chassis.Validate())
                return false;

            if (!Leg.Validate())
                return false;

            if (!Inventory.Validate())
                return false;

            return base.OnValidate(robot);
        }

        protected override void OnBuild(Robot robot)
        {
            foreach (var component in BuildComponents())
            {
                robot.AddChild(component);
            }

            base.OnBuild(robot);
        }

        public IList<ModuleTemplate> AllModuleInTemplate
        {
            get
            {
                var modules = new List<ModuleTemplate>();
                modules.AddRange(Head.Modules);
                modules.AddRange(Chassis.Modules);
                modules.AddRange(Leg.Modules);
                return modules;
            }
        }

        public int ItemScoreSum(Dictionary<int, int> scores)
        {
            var scoreSum = 0;
            foreach (var robotTemplateModule in AllModuleInTemplate)
            {
                if (scores.ContainsKey(robotTemplateModule.EntityDefault.Definition))
                {
                    scoreSum += scores[robotTemplateModule.EntityDefault.Definition];
                }
            }

            return scoreSum;
        }

        public List<Item> BuildComponents()
        {
            var head = Head.Build();
            var chassis = Chassis.Build();
            var leg = Leg.Build();
            var inventory = Inventory.Build();
            return new List<Item>
            {
                head,
                chassis,
                leg,
                inventory
            };
        }

     
    }
}
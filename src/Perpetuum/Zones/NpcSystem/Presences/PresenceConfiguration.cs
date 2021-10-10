using Perpetuum.IDGenerators;

namespace Perpetuum.Zones.NpcSystem.Presences
{
    public class PresenceConfiguration : IPresenceConfiguration
    {
        public int ID { get; private set; }

        public string Name { get; set; }
        public Area Area { get; set; }
        public int? SpawnId { get; set; }
        public string Note { get; set; }
        public bool Roaming { get; set; }
        public int RoamingRespawnSeconds { get; set; }
        public PresenceType PresenceType { get; set; }
        public int MaxRandomFlock { get; set; }

        public int? RandomCenterX { get; set; }
        public int? RandomCenterY { get; set; }
        public int? RandomRadius { get; set; }

        public int? DynamicLifeTime { get; set; }
        public bool IsRespawnAllowed { get; set; }

        public int? InterzoneGroupId { get; set; }

        public int? GrowthSeconds { get; set; }

        public int ZoneID { get; set; }

        public Position RandomCenter => new Position((double)RandomCenterX, (double)RandomCenterY);

        public PresenceConfiguration(int id, PresenceType presenceType)
        {
            ID = id;
            this.PresenceType = presenceType;
        }

        public override string ToString()
        {
            return $" ID:{ID} name:{Name} area:{Area} spawnID:{SpawnId} note:{Note}";
        }
    }

    public class DirectPresenceConfiguration : PresenceConfiguration
    {
        private static readonly IIDGenerator<int> _idGenerator = IDGenerator.CreateIntIDGenerator(25000);

        public DirectPresenceConfiguration(IZone zone) : base(_idGenerator.GetNextID(), PresenceType.Direct)
        {
            Area = zone.Configuration.Size.ToArea();
            Name = "direct presence " + ID;
            SpawnId = 10; //dynamic kamubol, szerintem kicsit sem kell
            Note = "abs! rulez";
        }
    }
}
using Perpetuum.IDGenerators;

namespace Perpetuum.Zones.NpcSystem.Presences
{
    public class PresenceConfiguration
    {
        public int ID { get; private set; }

        public string name;
        public Area area;
        public int? spawnId;
        public string note;
        public bool roaming; // kell-e meg?
        public int roamingRespawnSeconds;
        public readonly PresenceType presenceType;
        public int maxRandomFlock;

        public int? randomCenterX;
        public int? randomCenterY;
        public int? randomRadius;

        public int? dynamicLifeTime;
        public bool isRespawnAllowed;

        public Position RandomCenter => new Position((double) randomCenterX,(double) randomCenterY);

        public PresenceConfiguration(int id,PresenceType presenceType)
        {
            ID = id;
            this.presenceType = presenceType;
        }

        public override string ToString()
        {
            return $" ID:{ID} name:{name} area:{area} spawnID:{spawnId} note:{note}";
        }
    }

    public class DirectPresenceConfiguration : PresenceConfiguration
    {
        private static readonly IIDGenerator<int> _idGenerator = IDGenerator.CreateIntIDGenerator(25000);

        public DirectPresenceConfiguration(IZone zone) : base(_idGenerator.GetNextID(),PresenceType.Direct)
        {
            area = zone.Configuration.Size.ToArea();
            name = "direct presence " + ID;
            spawnId = 10; //dynamic kamubol, szerintem kicsit sem kell
            note = "abs! rulez";
        }
    }
}
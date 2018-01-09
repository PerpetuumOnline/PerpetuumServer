using System.Data;
using Perpetuum.Data;
using Perpetuum.ExportedTypes;

namespace Perpetuum.Zones.Artifacts
{
    /// <summary>
    /// Describes one artifact on the zone
    /// </summary>
    public class ArtifactInfo 
    {
        public readonly ArtifactType type;
        public readonly int goalRange;
        public readonly int? npcPresenceId;
        public readonly bool isPersistent;
        public readonly int minimumLoot;

        public ArtifactInfo(IDataRecord record)
        {
            type = record.GetValue<ArtifactType>("id");
            goalRange = record.GetValue<int>("goalrange");
            npcPresenceId = record.GetValue<int?>("npcpresenceid");
            isPersistent = record.GetValue<bool>("persistent");
            minimumLoot = record.GetValue<int>("minimumloot");

            

        }

        public override string ToString()
        {
            return string.Format("ArtifactInfo type:" + type);
        }

        public static ArtifactInfo GenerateArtifactInfo(IDataRecord record)
        {
            var persistentArtifact = record.GetValue<bool>("persistent");
            var isDynamic = record.GetValue<bool>("dynamic");
            if (persistentArtifact)
            {
                //normal persistent artifact
                return new ArtifactInfo(record);
            }

            if (isDynamic)
            {
                return new DynamicArtifactInfo(record);
            }

            //mission artifact, oldschool
            return new NonPersistentArtifactInfo(record);

        }

    }

    /// <summary>
    /// Mission artifactInfo, the oldschool version, with fixed npc presence
    /// </summary>
    public class NonPersistentArtifactInfo : ArtifactInfo
    {
        public NonPersistentArtifactInfo(IDataRecord record) : base(record)
        {
           
        }
    }

    /// <summary>
    /// Dynamic artifact info, npcs and stuff comes from random missions
    /// </summary>
    public class DynamicArtifactInfo : NonPersistentArtifactInfo
    {
        public DynamicArtifactInfo(IDataRecord record) : base(record)
        {
            
        }
    }


}
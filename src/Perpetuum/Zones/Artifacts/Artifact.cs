using System;
using System.Collections.Generic;
using Perpetuum.Accounting.Characters;

namespace Perpetuum.Zones.Artifacts
{
    /// <summary>
    /// This object represents one artifact found by a player on the zone
    /// </summary>
    public class Artifact
    {
        public Character Character { get; private set; }
        public Guid MissionGuid { get; set; }

        public Artifact(int id, ArtifactInfo info, Position position, Character character)
            : this(info, position, character)
        {
            Id = id;
        }

        public Artifact(ArtifactInfo info, Position position, Character character)
        {
            Character = character;
            Info = info;
            Position = position;
        }

        public int Id { get; private set; }

        public ArtifactInfo Info { get; private set; }

        public Position Position { get; private set; }

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
                                 {
                                         {k.ID, Id}, 
                                         {k.type, (int) Info.type},
                                         {k.position, Position}, 
                                         {k.goalRange, Info.goalRange}
                                 };
        }

        public override string ToString()
        {
            return $"Id: {Id}, Position: {Position} Type: {Info.type}";
        }
    }
}
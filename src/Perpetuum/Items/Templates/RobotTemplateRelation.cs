using Perpetuum.EntityFramework;

namespace Perpetuum.Items.Templates
{
    public class RobotTemplateRelation : IRobotTemplateRelation
    {
        private const int INVALID_HIGH_LEVEL = 1000; //olyan magas, h sosem fogja valasztani

        public int? missionLevel;
        public int? missionLevelOverride;

        public bool HasMissionLevel => missionLevel != null;
        public int MissionLevel => missionLevelOverride ?? (missionLevel ?? INVALID_HIGH_LEVEL);
        public EntityDefault EntityDefault { get; set; }
        public RobotTemplate Template { get; set; }
        public int RaceID { get; set; }
    }
}
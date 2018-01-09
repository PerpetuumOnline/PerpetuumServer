using Perpetuum.EntityFramework;

namespace Perpetuum.Items.Templates
{
    public interface IRobotTemplateRelation
    {
        EntityDefault EntityDefault { get; }
        RobotTemplate Template { get; }
        int RaceID { get; }
        bool HasMissionLevel { get; }
        int MissionLevel { get; }
    }
}
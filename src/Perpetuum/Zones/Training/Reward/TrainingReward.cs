using Perpetuum.Items;
using Perpetuum.Items.Templates;

namespace Perpetuum.Zones.Training.Reward
{
    public class TrainingReward
    {
        public int Level { get; private set; }
        public ItemInfo Item { get; private set; }
        [CanBeNull]
        public RobotTemplate RobotTemplate { get; private set; }
        public int RaceId { get; private set; }

        public TrainingReward(int level, ItemInfo item,RobotTemplate robotTemplate,int raceId)
        {
            Level = level;
            Item = item;
            RobotTemplate = robotTemplate;
            RaceId = raceId;
        }
    }
}

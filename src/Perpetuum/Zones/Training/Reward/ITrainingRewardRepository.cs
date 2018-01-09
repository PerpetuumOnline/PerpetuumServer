using System.Collections.Generic;

namespace Perpetuum.Zones.Training.Reward
{
    public interface ITrainingRewardRepository
    {
        IEnumerable<TrainingReward> GetAllRewards();
    }
}
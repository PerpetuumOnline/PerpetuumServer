using System.Collections.Generic;
using Perpetuum.Accounting.Characters;
using Perpetuum.EntityFramework;
using Perpetuum.Items;

namespace Perpetuum.Zones.Training.Reward
{
    public class TrainingRewardBuilder
    {
        private readonly Character _ownerCharacter;

        public TrainingRewardBuilder(Character ownerCharacter)
        {
            _ownerCharacter = ownerCharacter;
        }

        public IEnumerable<Item> Build(TrainingReward reward)
        {
            var result = new List<Item>();

            if (reward.Item.Definition > 0)
            {
                var item = (Item)Entity.Factory.CreateWithRandomEID(reward.Item.Definition);
                item.Quantity = reward.Item.Quantity;
                item.Owner = _ownerCharacter.Eid;
                result.Add(item);
            }

            var robot = reward.RobotTemplate?.Build();
            if (robot != null)
            {
                robot.Owner = _ownerCharacter.Eid;
                result.Add(robot);
            }

            return result;
        }
    }
}
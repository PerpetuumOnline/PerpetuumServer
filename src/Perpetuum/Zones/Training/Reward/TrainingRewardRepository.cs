using System.Collections.Generic;
using System.Linq;
using Perpetuum.Data;
using Perpetuum.Items;
using Perpetuum.Items.Templates;

namespace Perpetuum.Zones.Training.Reward
{
    public class TrainingRewardRepository : ITrainingRewardRepository
    {
        private readonly IRobotTemplateReader _robotTemplateReader;
        private static TrainingReward[] _rewards;

        public TrainingRewardRepository(IRobotTemplateReader robotTemplateReader)
        {
            _robotTemplateReader = robotTemplateReader;
        }

        public IEnumerable<TrainingReward> GetAllRewards()
        {
            return _rewards ?? (_rewards = Db.Query().CommandText("select * from trainingrewards")
                                                   .Execute()
                                                   .Select(r =>
                                                   {
                                                      var level = r.GetValue<int>("level");
                                                      var definition = r.GetValue<int>("definition");
                                                      var quantity = r.GetValue<int>("quantity");
                                                      var itemInfo = new ItemInfo(definition, quantity);

                                                      var robotTemplateId = r.GetValue<int>("robottemplateid");
                                                      var template = _robotTemplateReader.Get(robotTemplateId);

                                                      var raceId = r.GetValue<int>("raceid");

                                                      return new TrainingReward(level, itemInfo, template,raceId);
                                                   }).ToArray());
        }
    }
}
using System;
using System.Data;
using System.Linq;
using Perpetuum.Log;

namespace Perpetuum.Services.MissionEngine.Missions
{
    public class RandomMission : Mission
    {
        public RandomMission(IDataRecord record) : base(record) { }

        public override void AcceptVisitor(MissionVisitor visitor)
        {
            visitor.VisitRandomMission(this);
        }

        protected override bool CheckConsistency()
        {
            if (!ValidDifficultyMultiplierSet)
            {
                Logger.Error("no valid difficulty multiplier is set in " + this);
            }

            CheckForBrokenLinks();

            return base.CheckConsistency();
        }

        private void CheckForBrokenLinks()
        {
            foreach (var missionTarget in Targets)
            {
                if (missionTarget.ValidPrimaryLinkSet)
                {
                    if (!IsExistingIndex(missionTarget.PrimaryDefinitionLinkId))
                    {
                        Logger.Error("broken link. primary linked target was not found. " + missionTarget);
                    }
                }

                if (missionTarget.ValidSecondaryLinkSet)
                {
                    if (!IsExistingIndex(missionTarget.SecondaryDefinitionLinkId))
                    {
                        Logger.Error("broken link. secondary linked target was not found. " + missionTarget);
                    }
                }
            }
        }

        private bool IsExistingIndex(int index)
        {
            return Targets.Any(t => t.id == index);
        }

        protected override void LoadIssuer(IDataRecord record)
        {
            //skip issuer load, a random mission has no fixed issuer
        }

        public override string ToString()
        {
            var info = base.ToString();

            return info + " RND";
        }

        //                                              0   1   2   3   4    5    6    7    8    9
        private static readonly int[] CoinsPerLevel = { 10, 20, 40, 70, 120, 200, 310, 450, 620, 800};
        public static int CoinQuantity(int missionLevel, double difficultyMultiplier)
        {
            /*
            var rawAmount = Math.Pow(1.3 + missionLevel, 2.5) * 1.3;
            var multAmount = rawAmount * difficultyMultiplier;
            var amount = (int)Math.Round(multAmount);
             */

            missionLevel = missionLevel.Clamp(0, CoinsPerLevel.Length - 1);

            var rawAmount = CoinsPerLevel[missionLevel];
            var multAmount = rawAmount * difficultyMultiplier;
            var amount = (int) Math.Round(multAmount);

            return amount;
        }
       
    }
}

using System.Collections.Generic;
using Perpetuum.Players;

namespace Perpetuum.Zones.PBS.EffectNodes
{
    public class PBSMiningTower : PBSEffectEmitter, IPBSFeedable
    {
        public void FeedWithItems(Player player, IEnumerable<long> eids)
        {
            PBSHelper.FeedWithItems(this,player,eids);
        }
    }
}

using Perpetuum.Players;
using Perpetuum.Units;

namespace Perpetuum.Zones.Training
{
    public class TrainingKillSwitch : Unit,IUsableItem
    {
        public void UseItem(Player player)
        {
            if (CurrentPosition.TotalDistance2D(player.CurrentPosition) <= 3)
            {
                player.Kill();
            }
        }
    }
}

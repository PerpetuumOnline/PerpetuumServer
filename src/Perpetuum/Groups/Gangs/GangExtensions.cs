using Perpetuum.Players;
using Perpetuum.Units;

namespace Perpetuum.Groups.Gangs
{
    public static class GangExtensions
    {
        public static bool IsMember(this Gang gang, Unit target)
        {
            if (gang == null)
                return false;

            if (target is Player player)
                return gang == player.Gang;

            return false;
        }

        public static bool IsMember(this Gang gang,Player player)
        {
            if (gang == null)
                return false;

            return player != null && gang.IsMember(player.Character);
        }
    }
}
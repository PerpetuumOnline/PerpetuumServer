using System.Linq;
using Perpetuum.ExportedTypes;
using Perpetuum.Units;
using Perpetuum.Zones.DamageProcessors;

namespace Perpetuum.Zones.Intrusion
{
    /// <summary>
    /// Intrusion target which can be completed by destroying the SAP
    /// </summary>
    public class DestructionSAP : SAP
    {
        public DestructionSAP() : base(BeamType.attackpoint_damage_enter, BeamType.attackpoint_damage_out)
        {
        }

        protected override void OnDamageTaken(Unit source, DamageTakenEventArgs e)
        {
            base.OnDamageTaken(source, e);

            var player = Zone.ToPlayerOrGetOwnerPlayer(source);
            if (player == null)
                return;

            IncrementPlayerScore(player, (int)e.TotalDamage);
        }

        protected override void OnDead(Unit killer)
        {
            OnTakeOver();
            base.OnDead(killer);
        }

        protected override int MaxScore => 0;

        protected override void AppendTopScoresToPacket(Packet packet,int count)
        {
            var topScores = GetCorporationTopScores(count);

            packet.AppendInt(topScores.Count);
            packet.AppendByte(sizeof(long));

            foreach (var topScore in topScores)
            {
                packet.AppendLong(topScore.corporationEid);
                packet.AppendInt(topScore.score);
            }
        }

        public override long GetWinnerCorporationEid()
        {
            var score = GetCorporationTopScores(1).FirstOrDefault();
            return score.corporationEid;
        }
    }
}
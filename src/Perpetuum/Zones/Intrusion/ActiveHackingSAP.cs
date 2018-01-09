using Perpetuum.ExportedTypes;
using Perpetuum.Players;
using Perpetuum.Zones.Beams;

namespace Perpetuum.Zones.Intrusion
{
    /// <summary>
    /// Intrusion target which can be completed by using a hacking module on the SAP
    /// </summary>
    public class ActiveHackingSAP : SAP
    {
        private const int MAX_MODULE_CYCLE = 120;
        
        public ActiveHackingSAP() : base(BeamType.attackpoint_usage_enter, BeamType.attackpoint_usage_out)
        {
        }

        protected override int MaxScore => MAX_MODULE_CYCLE;

        protected override void AppendTopScoresToPacket(Packet packet,int count)
        {
            AppendPlayerTopScoresToPacket(this,packet,count);
        }

        public void OnModuleUse(Player player)
        {
            Zone.CreateBeam(BeamType.loot_bolt,b => b.WithSource(this)
                                                          .WithTarget(player)
                                                          .WithState(BeamState.Hit)
                                                          .WithDuration(3000));
            IncrementPlayerScore(player, 1);
        }
    }
}
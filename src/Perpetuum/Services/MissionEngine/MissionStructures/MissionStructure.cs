using System;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Players;
using Perpetuum.Units;
using Perpetuum.Zones;
using Perpetuum.Zones.Beams;

namespace Perpetuum.Services.MissionEngine.MissionStructures
{

   



    /// <summary>
    /// 
    /// Handles success and interaction beams
    /// 
    /// </summary>
    public abstract class MissionStructure : Unit
    {
        private readonly MissionTargetType _targetType;
        protected MissionStructure(MissionTargetType targetType)
        {
            _targetType = targetType;
        }

        public MissionTargetType TargetType { get { return _targetType; } }

        private bool IsBeamPublic()
        {
            return ED.Options.PublicBeam;
        }

        public void CreateSuccessBeam(Player player)
        {
            //success beam by alarm switch definition
            var builder = Beam.NewBuilder().WithType(BeamHelper.GetBeamByDefinition(Definition))
                .WithPosition(CurrentPosition)
                .WithState(BeamState.AlignToTerrain)
                .WithDuration(TimeSpan.FromSeconds(3));

            if (IsBeamPublic())
            {
                //mindenki latja
                Zone.CreateBeam(builder);
            }
            else
            {
                //csak 1 player latja
                player.Session.SendBeam(builder);
            }
        }

        protected void CreateInteractionBeam(Player player)
        {
            //interaction beam
            Zone.CreateBeam(BeamType.loot_bolt, b => b.WithSource(player)
                .WithTarget(this)
                .WithState(BeamState.Hit)
                .WithDuration(1000));
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }
    }

}

using System.Collections.Generic;
using Perpetuum.Players;
using Perpetuum.Zones;

namespace Perpetuum.Services.MissionEngine.MissionStructures
{

    /// <summary>
    /// 
    /// Checks distance
    /// Matches mission type and position
    /// Creates interaction beam
    /// 
    /// </summary>
    public abstract class MissionSwitch : MissionStructure
    {
        protected MissionSwitch(MissionTargetType targetType = MissionTargetType.use_switch) : base(targetType) { }

        public void CanUseAndCheckError(Player player)
        {
            var ec =CanUse(player).ThrowIfError();

           
            /*
            switch (ec)
            {
                case ErrorCodes.NoError:
                case ErrorCodes.MissionNoSwitchTargetActive:
                    return;

                default:
                    throw new GenxyException(ec);
            }
            */
        }

        public virtual Dictionary<string, object> GetUseResult()
        {
            var result = BaseInfoToDictionary();
            return result;
        }


        public ErrorCodes CanUse(Player player)
        {
            if (!IsInRangeOf3D(player, DistanceConstants.SWITCH_USE_DISTANCE))
            {
                return ErrorCodes.ItemOutOfRange;
            }

            //only with related mission target running
            var structureTargets = player.MissionHandler.GetTargetsForMissionStructure(this);

            if (structureTargets.Count == 0)
            {
                return ErrorCodes.MissionNoSwitchTargetActive;
            }

            return ErrorCodes.NoError;

        }

        public virtual void Use(Player player)
        {
            CanUse(player).ThrowIfError();
            CreateInteractionBeam(player);
        }

        protected static void CheckErrorAndOmitSwitchActive(ErrorCodes ec)
        {
            switch (ec)
            {
                case ErrorCodes.NoError:
                case ErrorCodes.MissionNoSwitchTargetActive:
                    return;

                default:
                    throw new PerpetuumException(ec);
            }

        }
        
    }

}

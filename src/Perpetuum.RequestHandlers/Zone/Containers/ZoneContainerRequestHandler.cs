using System.Linq;
using Perpetuum.Containers;
using Perpetuum.Host.Requests;
using Perpetuum.Modules;
using Perpetuum.Players;
using Perpetuum.Units.FieldTerminals;
using Perpetuum.Zones;

namespace Perpetuum.RequestHandlers.Zone.Containers
{
    public abstract class ZoneContainerRequestHandler : IRequestHandler<IZoneRequest>
    {
        public abstract void HandleRequest(IZoneRequest request);

        protected static ErrorCodes CheckContainerType(Container container, int issuerCharacterId = 0)
        {
            if (container is VolumeWrapperContainer wrapperContainer)
            {
                if (issuerCharacterId == 0)
                    return ErrorCodes.AccessDenied;

                if (wrapperContainer.PrincipalCharacter.Id == issuerCharacterId)
                    return ErrorCodes.NoError;

                return ErrorCodes.AccessDenied;
            }

            return ErrorCodes.NoError;
        }

        protected static ErrorCodes CheckPvpState(Player player)
        {
            if (player.HasPvpEffect)
                return ErrorCodes.CantBeUsedInPvp;

            return ErrorCodes.NoError;
        }

        protected static ErrorCodes CheckCombatState(Player player)
        {
            if (player.States.Combat)
                return ErrorCodes.OperationNotAllowedInCombat;

            return ErrorCodes.NoError;
        }

        protected static ErrorCodes CheckFieldTerminalRange(Player player, Container container)
        {
            var fieldTerminal = container.ParentEntity as FieldTerminal;
            if (fieldTerminal == null)
                return ErrorCodes.NoError;

            var inRange = player.IsInRangeOf3D(fieldTerminal, DistanceConstants.FIELD_TERMINAL_USE);
            if (!inRange)
                return ErrorCodes.ItemOutOfRange;

            return ErrorCodes.NoError;
        }

        protected static ErrorCodes CheckActiveModules(Player player)
        {
            var hasNotIdleModule = player.ActiveModules.Any(m => m.State.Type != ModuleStateType.Idle);
            if (hasNotIdleModule)
                return ErrorCodes.AllModulesHasToBeIdle;

            return ErrorCodes.NoError;
        }
    }
}
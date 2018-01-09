using Perpetuum.Accounting.Characters;
using Perpetuum.Host.Requests;
using Perpetuum.Items;

namespace Perpetuum.RequestHandlers
{
    public class Undock : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;

            if (!request.Session.AccessLevel.IsAdminOrGm())
                CheckUndockConditionsAndThrowIfFailed(character);

            var dockingBase = character.GetCurrentDockingBase();
            if (dockingBase == null)
                throw new PerpetuumException(ErrorCodes.DockingBaseNotFound);

            if (dockingBase.Zone == null)
                throw new PerpetuumException(ErrorCodes.ItemNotFound);

            dockingBase.Zone.Enter(character,Commands.Undock);
        }

        private static void CheckUndockConditionsAndThrowIfFailed(Character character)
        {
            character.CheckNextAvailableUndockTimeAndThrowIfFailed();

            var activeRobot = character.GetActiveRobot().ThrowIfNull(ErrorCodes.ARobotMustBeSelected);
            activeRobot.CheckEnablerExtensionsAndThrowIfFailed(character);
            activeRobot.CheckEnergySystemAndThrowIfFailed();
            var container = activeRobot.GetContainer().ThrowIfNull(ErrorCodes.WTFErrorMedicalAttentionSuggested);
            container.CheckCapacityAndThrowIfFailed();
        }
    }
}

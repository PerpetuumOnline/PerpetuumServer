using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Log;
using Perpetuum.Services.Sessions;
using Perpetuum.Units.DockingBases;

namespace Perpetuum.RequestHandlers
{
    /// <summary>
    /// Docks player to TMA
    /// </summary>
    public class ForceDock : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                Logger.Info("player forced dock. characterID:" + character.Id);

                Db.Query().CommandText("characterDockToTMA")
                    .SetParameter("@characterId", character.Id)
                    .ExecuteNonQuery();

                Message.Builder.FromRequest(request).WithError(ErrorCodes.YouAreHappyNow).Send();
                
                scope.Complete();
            }
        }
    }

    public class ForceDockAdmin : IRequestHandler
    {
        private readonly ISessionManager _sessionManager;
        private readonly DockingBaseHelper _dockingBaseHelper;

        public ForceDockAdmin(ISessionManager sessionManager, DockingBaseHelper dockingBaseHelper)
        {
            _sessionManager = sessionManager;
            _dockingBaseHelper = dockingBaseHelper;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var id = request.Data.GetOrDefault<int>(k.characterID);
                var characterSession = _sessionManager.GetByCharacter(id);
                var Zone = characterSession.Character.GetCurrentZone();

                var baseEid = 561; // TMA.
                var dockingbase = _dockingBaseHelper.GetDockingBase(baseEid);
                var player = Zone.GetPlayer(characterSession.Character.GetActiveRobot().Eid);
                player.DockToBase(Zone, dockingbase);

                Message.Builder.ToClient(characterSession).WithError(ErrorCodes.YouAreHappyNow).Send();

                scope.Complete();
            }
        }
    }
}
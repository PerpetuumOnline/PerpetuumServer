using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Log;

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
}
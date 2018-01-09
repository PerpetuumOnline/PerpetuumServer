using Perpetuum.Host.Requests;
using Perpetuum.Log;
using Perpetuum.Services.Sessions;

namespace Perpetuum.RequestHandlers
{
    /// <remarks>
    /// After the socket disconnects it fires a disconnect event which will deal with the register, user, etc.
    /// </remarks>
    public class SetMaxUserCount : IRequestHandler
    {
        private readonly ISessionManager _sessionManager;

        public SetMaxUserCount(ISessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }

        public void HandleRequest(IRequest request)
        {
            var amount = request.Data.GetOrDefault<int>(k.amount);
            _sessionManager.MaxSessions = amount;
            Logger.Info($"MaxUsers = {_sessionManager.MaxSessions}");
            Message.Builder.FromRequest(request).WithOk().Send();
        }
    }

}
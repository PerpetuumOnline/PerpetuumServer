using Perpetuum.Accounting.Characters;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Sessions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Perpetuum.RequestHandlers.AdminTools
{
    public class CharactersOnline : IRequestHandler
    {

        private readonly ISessionManager _sessionManager;

        public CharactersOnline(ISessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }

        public void HandleRequest(IRequest request)
        {
            var sessions = _sessionManager.Sessions;

            var x = sessions.ToDictionary("s", s =>
            {
                var d = new Dictionary<string, object>
                {
                    [k.accessLevel] = (int)s.AccessLevel,
                    [k.accountID] = s.AccountId,
                    [k.characterID] = (s.Character == Character.None) ? 0 : s.Character.Id,
                    [k.nick] = (s.Character == Character.None) ? "No Character" : s.Character.Nick,
                    [k.zoneID] = (s.Character == Character.None) ? 0 : s.Character.ZoneId,
                    [k.docked] = (s.Character == Character.None) ? false : s.Character.IsDocked,
                    [k.name] = (s.Character.GetCurrentDockingBase() is null) ? "Unknown" : s.Character.GetCurrentDockingBase().Name,
                    [k.position] = (s.Character.GetPlayerRobotFromZone() != null) ? s.Character.GetPlayerRobotFromZone().CurrentPosition : new Position(),
                    [k.steambuildid] = s.SteamBuild,
                    [k.clientver] = s.ClientVersion,
                    [k.ip] = s.RemoteEndPoint.Address.ToString()
                };
                return d;
            });

            Message.Builder.FromRequest(request).WithData(x).Send();
        }
    }
}
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Perpetuum.Accounting;
using Perpetuum.Accounting.Characters;
using Perpetuum.Common;
using Perpetuum.Data;
using Perpetuum.Host;
using Perpetuum.Log;
using Perpetuum.Network;
using Perpetuum.Services.Relay;
using Perpetuum.Services.Steam;

namespace Perpetuum.Services.Sessions
{
    public class SessionManager : ISessionManager
    {
        private readonly TcpListener _listener;
        private readonly Session.Factory _sessionFactory;
        private readonly IRelayStateService _relayStateService;
        private readonly ISteamManager _steamManager;
        private readonly GlobalConfiguration _globalConfiguration;
        private readonly ConcurrentDictionary<SessionID,ISession>  _sessions = new ConcurrentDictionary<SessionID, ISession>();
        private readonly ConcurrentDictionary<Character,ISession>  _charactersIndex = new ConcurrentDictionary<Character, ISession>();

        public SessionManager(GlobalConfiguration globalConfiguration,IHostStateService hostStateService,Session.Factory sessionFactory,IRelayStateService relayStateService,ISteamManager steamManager)
        {
            var relayEndPoint = new IPEndPoint(IPAddress.Any,globalConfiguration.ListenerPort);

            _listener = new TcpListener(relayEndPoint);
            _globalConfiguration = globalConfiguration;
            _sessionFactory = sessionFactory;
            _relayStateService = relayStateService;
            _steamManager = steamManager;
            MaxSessions = 1000;

            hostStateService.StateChanged += (sender,state) =>
            {
                switch (state)
                {
                    case HostState.Online:
                    {
                        _listener.Start(OnConnectionAccepted);
                        break;
                    }
                    case HostState.Off:
                    {
                        Stop();
                        break;
                    }
                }
            };
        }

        public IEnumerable<ISession> Sessions => _sessions.Values;

        public IEnumerable<Character> SelectedCharacters => _charactersIndex.Keys;

        private void OnConnectionAccepted(Socket socket)
        {
            Logger.Info($"[Relay] client connected. {socket.RemoteEndPoint}");

            var session = _sessionFactory(socket);
            session.Disconnected += OnSessionDisconnected;
            session.RsaKeyReceived += OnSessionRsaKeyReceived;
            Add(session);

            session.Start();
        }

        private void OnSessionRsaKeyReceived(ISession session)
        {
            var welcomeData = new Dictionary<string, object>
            {
                {k.worldName, "Perpetuum"},
#if DEBUG
                {"dev",true},
#endif
                {"version",GenxyVersion.REVISION},
                {k.OSTime,DateTime.Now},
                {"steamLoginEnabled",_steamManager.SteamAppID > 0}
            };

            if (!string.IsNullOrEmpty(_globalConfiguration.ResourceServerURL))
            {
                welcomeData.Add("resourceServerURL", _globalConfiguration.ResourceServerURL);
            }

            var builder = Message.Builder.SetCommand(Commands.Welcome).WithData(welcomeData);
            session.SendMessage(builder);
        }

        private static void OnSessionDisconnected(ISession session)
        {
            Logger.Info($"[Relay] client disconnected. {session.RemoteEndPoint}");

            using (var scope = Db.CreateTransaction())
            {
                session.SignOut();
                scope.Complete();
            }
        }

        public void Start()
        {
        }

        public void Stop()
        {
            _listener.Stop();

            foreach (var session in _sessions.Values)
            {
                session.ForceQuit(ErrorCodes.ServerDisconnects);
            }
        }

        private void Add(ISession session)
        {
            session.Disconnected += Remove;
            session.CharacterSelected += (s, selected) =>
            {
                _charactersIndex[selected] = s;
            };
            session.CharacterDeselected += (s, selected) =>
            {
                _charactersIndex.Remove(selected);

                CharacterDeselected?.Invoke(session,selected);
            };

            _sessions[session.Id] = session;
            OnSessionAdded(session);
        }

        private void Remove(ISession session)
        {
            _sessions.Remove(session.Id);
        }

        public bool Contains(SessionID id)
        {
            return _sessions.ContainsKey(id);
        }

        [CanBeNull]
        public ISession Get(SessionID id)
        {
            return !_sessions.TryGetValue(id, out ISession session) ? null : session;
        }

        [CanBeNull]
        public ISession GetByCharacter(Character character)
        {
            if (character == Character.None)
                return null;

            return !_charactersIndex.TryGetValue(character, out ISession session) ? null : session;
        }

        [CanBeNull]
        public ISession GetByAccount(Account account)
        {
            return GetByAccount(account.Id);
        }

        [CanBeNull]
        public ISession GetByAccount(int accountId)
        {
            if (accountId == 0)
                return null;

            return _sessions.Values.FirstOrDefault(s => s.AccountId == accountId);
        }

        [CanBeNull]
        public ISession GetByCharacter(int characterid)
        {
            if (characterid == 0)
            {
                return null;
            }
            return _sessions.Values.FirstOrDefault(s => s.Character.Id == characterid);
        }


        public bool IsOnline(Character character)
        {
            return _charactersIndex.ContainsKey(character);
        }

        public int MaxSessions { get; set; }

        public event SessionEventHandler SessionAdded;

        private void OnSessionAdded(ISession session)
        {
            _relayStateService.SendStateToClient(session);
            SessionAdded?.Invoke(session);
        }

        public event SessionEventHandler<Character> CharacterDeselected;
    }
}
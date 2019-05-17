using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Transactions;
using Perpetuum.Accounting;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.GenXY;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;
using Perpetuum.Log;
using Perpetuum.Network;
using Perpetuum.Services.Social;
using Perpetuum.Zones;

namespace Perpetuum.Services.Sessions
{
    public delegate void SessionEventHandler(ISession session);
    public delegate void SessionEventHandler<in T>(ISession session, T args);

    public interface ISession
    {
        SessionID Id { get; }
        int AccountId { get; }
        IPEndPoint RemoteEndPoint { get; }
        bool IsAuthenticated { get; }
        Character Character { get; }
        AccessLevel AccessLevel { get; }
        IZoneManager ZoneMgr { get; }
        bool AccountCreatedInSession { get; set; }
        string ClientVersion { get; set; }
        int SteamBuild { get; set; }

        void SendMessage(MessageBuilder builder);
        void SendMessage(IMessage message);
        // to expose these to our chat command interface.
        IRequest CreateLocalRequest(string data);
        void HandleLocalRequest(IRequest request);

        void Start();

        void SignIn(int accountID, string hwHash, int language);
        void SignOut();

        void SelectCharacter(Character character);
        void DeselectCharacter();

        void ForceQuit(ErrorCodes error = ErrorCodes.NoError, string comment = null);

        event SessionEventHandler Disconnected;
        event SessionEventHandler RsaKeyReceived;
        event SessionEventHandler<Character> CharacterSelected;
        event SessionEventHandler<Character> CharacterDeselected;
    }

    public class Session : ISession
    {
        private readonly GlobalConfiguration _globalConfiguration;
        private readonly IAccountManager _accountManager;
        private readonly IZoneManager _zoneManager;
        private readonly ICustomDictionary _customDictionary;
        private readonly Func<string, Command> _commandFactory;
        private readonly RequestHandlerFactory<IRequest> _requestHandlerFactory;
        private readonly RequestHandlerFactory<IZoneRequest> _zoneRequestHandlerFactory;

        private readonly ITcpConnection _connection;

        private AccessLevel _accessLevel;
        private DateTime _characterSelectTime;
        private DateTime _sessionStart = DateTime.Now;

        public delegate ISession Factory(Socket socket);

        public Session(GlobalConfiguration globalConfiguration,
            IAccountManager accountManager,
            IZoneManager zoneManager,
            ICustomDictionary customDictionary,
            Socket socket,
            Func<string, Command> commandFactory,
            RequestHandlerFactory<IRequest> requestHandlerFactory,
            RequestHandlerFactory<IZoneRequest> zoneRequestHandlerFactory)
        {
            Id = SessionID.New();

            _connection = new SessionConnection(socket) { RsaKeyReceived = OnRsaKeyReceived };
            _connection.Disconnected += OnDisconnected;
            _connection.Received += OnDataReceived;
            _globalConfiguration = globalConfiguration;
            _accountManager = accountManager;
            _zoneManager = zoneManager;
            _customDictionary = customDictionary;
            _commandFactory = commandFactory;
            _requestHandlerFactory = requestHandlerFactory;
            _zoneRequestHandlerFactory = zoneRequestHandlerFactory;

            _accessLevel = AccessLevel.notDefined;
        }

        public AccessLevel AccessLevel => _accessLevel;
        public string ClientVersion { get; set; } = string.Empty;
        public int SteamBuild { get; set; } = 0;

        private bool SafeLogOut { get; set; }

        private TimeSpan SessionTime
        {
            get { return Character != Character.None ? DateTime.Now.Subtract(_characterSelectTime) : TimeSpan.Zero; }
        }

        public void Start()
        {
            _connection.Receive();
        }

        public IZoneManager ZoneMgr
        {
            get
            {
                return _zoneManager;
            }
        }

        public SessionID Id { get; private set; }

        public int AccountId { get; private set; }

        public Character Character { get; private set; } = Character.None;

        public IPEndPoint RemoteEndPoint => _connection.RemoteEndPoint;

        private TimeSpan OnlineTime => DateTime.Now.Subtract(_sessionStart);

        public bool IsAuthenticated => AccountId > 0;

        public event SessionEventHandler<Character> CharacterSelected;

        public event SessionEventHandler<Character> CharacterDeselected;

        public event SessionEventHandler Disconnected;

        public event SessionEventHandler RsaKeyReceived;
        
        /// <summary>
        /// mitigate account creation spam. now must disconnect before trying to make another account.
        /// </summary>
        public bool AccountCreatedInSession { get; set; }

        public void SendMessage(MessageBuilder messageBuilder)
        {
            var message = messageBuilder.Build();
            SendMessage(message);
        }

        public void SendMessage(IMessage message)
        {
            _connection.Send(message.ToBytes());
        }

        public void SelectCharacter(Character character)
        {
            Character = character;
            _characterSelectTime = DateTime.Now;

            OnCharacterSelected(character);
        }

        public void DeselectCharacter()
        {
            var selectedCharacter = Character;
            if (selectedCharacter == Character.None)
                return;

            selectedCharacter.LastLogout = DateTime.Now;
            selectedCharacter.TotalOnlineTime += SessionTime;
            selectedCharacter.IsOnline = false;

            Transaction.Current.OnCommited(() =>
            {
                OnCharacterDeselected(selectedCharacter);

                Character = Character.None;
                _accessLevel = AccessLevel.normal;
            });
        }

        public void Disconnect(bool safeLogout)
        {
            SafeLogOut = safeLogout;
            ForceQuit(ErrorCodes.NoSimultaneousLoginsAllowed);
        }

        public void ForceQuit(ErrorCodes error = ErrorCodes.NoError, string comment = null)
        {
            Logger.Info("force disconnect on: sessionId:" + Id + " accountID:" + AccountId + " character:" + Character + " ec:" + error);
            SendMessage(Message.Builder.SetCommand(Commands.Quit).WithError(error).SetData(k.comment, comment));
            _connection.Disconnect();
        }

        public void SignIn(int accountID, string hwHash, int language)
        {
            if (IsAuthenticated)
                return;

            using (var scope = Db.CreateTransaction())
            {
                var account = _accountManager.Repository.Get(accountID).ThrowIfNull(ErrorCodes.AccountNotFound);

                Db.Query().CommandText("update characters set inuse=0 where accountid=@id")
                    .SetParameter("@id", account.Id)
                    .ExecuteNonQuery();

                Db.Query().CommandText("accountonlinetimestart")
                    .SetParameter("@accountId", account.Id)
                    .SetParameter("@ip", _connection.RemoteEndPoint.Address.ToString())
                    .SetParameter("@hwHash", hwHash)
                    .SetParameter("@isTrial", false)
                    .ExecuteNonQuery().ThrowIfZero(ErrorCodes.SQLExecutionError);

                account.IsLoggedIn = true;
                account.LastLoggedIn = DateTime.Now;

                _accountManager.PackageGenerateAll(account);

                _accountManager.Repository.Update(account);

                Transaction.Current.OnCommited(() =>
                {
                    Logger.Info($"Sign in >>| AccountId = {account.Id} Email = {account.Email}");

                    _accessLevel = account.AccessLevel | AccessLevel.normal;
                    _sessionStart = DateTime.Now; //every new account starts it's own _session

                    AccountId = account.Id;

                    var response = account.ToDictionary();
                    var customDictionary = _customDictionary.GetDictionary(language);
                    if (customDictionary != null && customDictionary.Count > 0)
                    {
                        response.Add(k.customDictionary, customDictionary);
                    }

                    var builder = Message.Builder.SetCommand(Commands.SignIn).WithData(response);
                    SendMessage(builder);
                });

                scope.Complete();
            }
        }

        public void SignOut()
        {
            if (!IsAuthenticated)
                return;

            DeselectCharacter();

            var account = _accountManager.Repository.Get(AccountId).ThrowIfNull(ErrorCodes.AccountNotFound);

            Db.Query().CommandText("accountonlinetimestop")
                .SetParameter("@accountId", account.Id)
                .SetParameter("@safeLogout", SafeLogOut)
                .ExecuteNonQuery().ThrowIfZero(ErrorCodes.SQLExecutionError);

            account.IsLoggedIn = false;
            account.TotalOnlineTime += OnlineTime;

            _accountManager.Repository.Update(account);

            Transaction.Current.OnCommited(() =>
            {
                Logger.Info($"Sign out <<| AccountId =  {account.Id} Email = {account.Email}");

                AccountId = 0;
                _accessLevel = AccessLevel.notDefined;
                Character = Character.None;
            });
        }

        private void OnCharacterSelected(Character character)
        {
            Logger.Info($"Character selected \\o/ characterId:{character.Id} characterEid:{character.Eid}");
            character.GetSocial().SendOnlineStateToFriends(true);
            CharacterSelected?.Invoke(this, character);
        }

        private void OnCharacterDeselected(Character character)
        {
            Logger.Info($"Character deselected /M\\ character:{character}");

            var m = Message.Builder.SetCommand(Commands.CharacterDeselect).WithOk();
            SendMessage(m);

            character.GetSocial().SendOnlineStateToFriends(false);
            CorporationDocumentHelper.RemoveFromAllDocuments(character);

            CharacterDeselected?.Invoke(this, character);
        }

        private void OnDisconnected(ITcpConnection connection)
        {
            using (var scope = Db.CreateTransaction())
            {
                SignOut();
                scope.Complete();
            }

            Disconnected?.Invoke(this);
        }

        private void OnRsaKeyReceived()
        {
            RsaKeyReceived?.Invoke(this);
        }

        private void OnDataReceived(ITcpConnection connection, byte[] receivedData)
        {
            var inString = Encoding.UTF8.GetString(receivedData);
            IRequest request = null;
            try
            {
                request = CreateRequest(inString);
                HandleRequest(request);
            }
            catch (Exception ex)
            {
                if (ex is PerpetuumException pex)
                {
                    var e = new LogEvent
                    {
                        LogType = LogType.Error,
                        Tag = "UREQ",
                        Message = $"{pex} ip: {RemoteEndPoint.Address} account: {AccountId} character: {Character} Req: {inString}"
                    };

                    Logger.Log(e);
                }
                else
                {
                    Logger.Exception(ex);
                }

                SendMessage(Message.Builder.WithException(ex).SetCommand(request?.Command));
            }
        }

        public void HandleLocalRequest(IRequest request)
        {
            HandleRequest(request);
        }

        private void HandleRequest(IRequest request)
        {
            if (request is IZoneRequest zoneRequest)
            {
                var handler = _zoneRequestHandlerFactory(zoneRequest.Command);
                if (handler != null)
                {
                    handler.HandleRequest(zoneRequest);
                    return;
                }
            }

            var requestHandler = _requestHandlerFactory(request.Command);
            requestHandler.HandleRequest(request);
        }

        public IRequest CreateLocalRequest(string data)
        {
            return CreateRequest(data);
        }

        private IRequest CreateRequest(string data)
        {
            var args = data.Split(':');
            if (args.Length < 3)
                throw new PerpetuumException(ErrorCodes.TooManyOrTooFewArguments);

            var commandText = args[0];

            var command = _commandFactory(commandText);
            if (command == null)
                throw PerpetuumException.Create(ErrorCodes.NoSuchCommand).SetData("command", commandText);

            if (!_accessLevel.HasFlag(command.AccessLevel))
            {
                throw PerpetuumException.Create(ErrorCodes.InsufficientPrivileges)
                    .SetData("command", command.Text)
                    .SetData("accessLevel", (int)_accessLevel);
            }

            var targetPlugin = args[1];

            var dictionary = GenxyConverter.Deserialize(args[2]);
            command.CheckArguments(dictionary);

            var request = new Request
            {
                Command = command,
                Session = this,
                Target = _globalConfiguration.RelayName,
                Data = dictionary,
            };

            if (!targetPlugin.StartsWith("zone_"))
                return request;

            request.Target = targetPlugin;
            var zoneID = int.Parse(targetPlugin.Remove(0, 5));
            var zr = new ZoneRequest(request)
            {
                Zone = _zoneManager.GetZone(zoneID)
            };

            return zr;
        }

        private class SessionConnection : EncryptedTcpConnection
        {
            private Rc4 _rc4;

            public SessionConnection(Socket socket) : base(socket)
            {
            }

            public Action RsaKeyReceived { private get; set; }

            protected override void OnReceived(byte[] data)
            {
                if (_rc4 == null)
                {
                    HandleRsaPacket(data);
                    return;
                }

                var tmpBuffer = new byte[data.Length - 1];
                Buffer.BlockCopy(data, 1, tmpBuffer, 0, data.Length - 1);

                _rc4.Decrypt(ref tmpBuffer);

                var x = new byte[tmpBuffer.Length - 3];
                Buffer.BlockCopy(tmpBuffer, 3, x, 0, x.Length);
                base.OnReceived(x);
            }

            private void HandleRsaPacket(byte[] data)
            {
                var rsaData = new byte[data.Length - 4];
                Buffer.BlockCopy(data, 4, rsaData, 0, data.Length - 4);

                var streamKey = Rsa.Decrypt(rsaData).ThrowIfNull(ErrorCodes.WTFErrorMedicalAttentionSuggested);

                _rc4 = new Rc4(streamKey);

                RsaKeyReceived();
            }

            public override void Send(byte[] data)
            {
                if (_rc4 == null)
                    return;

                base.Send(data);
            }

            protected override byte[] OnProcessOutputRawData(byte[] data)
            {
                var compressionLevel = 0;
                var outPacket = new byte[data.Length + 3];

                Buffer.BlockCopy(data, 0, outPacket, 3, data.Length);

                if (outPacket.Length > 1024)
                {
                    compressionLevel = 2;
                    outPacket = GZip.Compress(outPacket);
                }

                _rc4.Encrypt(outPacket);

                var outputBytes = new byte[outPacket.Length + 1];
                Buffer.BlockCopy(outPacket, 0, outputBytes, 1, outPacket.Length);

                //write compression level
                outputBytes[0] = (byte)compressionLevel;

                return outputBytes;
            }
        }
    }
}
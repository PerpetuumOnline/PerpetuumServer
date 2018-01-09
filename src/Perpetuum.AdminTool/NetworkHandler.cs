using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Perpetuum.Network;

namespace Perpetuum.AdminTool
{
    public class NetworkHandler
    {
        private readonly LogHandler _log;
        private readonly AdminCreds _adminCreds;
        public bool IsDefaultUser { get; private set; } = true;

        public NetworkHandler(LogHandler logHandler, AdminCreds adminCreds)
        {
            _log = logHandler;
            _adminCreds = adminCreds;
        }

        public event Action<LoginState> LoginStateChanged;
        public event Action<ErrorCodes> Error;

        private ClientConnection _connection;
        public LoginState LoginState { get; private set; }

        private bool _connected;

        public event Action<bool> ConnectionStateChanged;

        public bool Connected
        {
            get => _connected;
            set
            {
                if (_connected == value)
                    return;
                _connected = value;
                ConnectionStateChanged?.Invoke(_connected);

            }
        }


        private bool Connect()
        {
            try { _connection = ClientConnection.Connect(new IPEndPoint(IPAddress.Parse(_adminCreds.Ip), int.Parse(_adminCreds.Port))); }
            catch (Exception ex)
            {
                _log.Log(ex.Message);
                _log.StatusError($"unable to connect to. {_adminCreds.Ip}");
                OnLoginStateChanged(LoginState.UnableToConnect);
                Reset();
                return false;
            }

            _connection.Disconnected += OnDisconnect;
            _connection.Received += OnReceieved;
            _connection.MessageReceived += OnMessageReceived;
            _connection.SendHandshakeAsync();
            _log.Log($"connected to {_adminCreds.Ip}:{_adminCreds.Port}");
            Connected = true;
            return true;
        }

        private bool _defaultUserTest;
        private AdminCreds _creds;

        public bool TestDefaultAccount()
        {
            if (!Connected) return true;
            try
            {
                _log.Log("++ Login using default account start");
                _defaultUserTest = true;
                var m = Login(AdminCreds.DefaultAdminCreds);
                IsDefaultUser = !m.Data.ContainsKey(k.error);
                _log.Log("++ Login using default account end");
            }
            catch (Exception) { }
            finally { _defaultUserTest = false; }

            return IsDefaultUser;
        }

        private IMessage Login(AdminCreds creds)
        {
            _log.Log("++ login start");
             
            var t = LoginAsync(creds);
            var m = t.Result;
            LoginResult(m.Data);
            _log.Log("++ login end");
            return m;
        }

        private void OnDisconnect(ITcpConnection connection)
        {
            _log.Log("disconnected from server");
            _log.StatusMessage("disconnected from server.");
            Reset();
            OnLoginStateChanged(LoginState.Disconnected);
        }

        private void Reset()
        {
            Connected = false;
            IsDefaultUser = true;
            LoginState = LoginState.Unknown;
        }

        public void Disconnect()
        {
            if (_connection == null || !Connected) return;
            _connection.Disconnect();
            _connection.Disconnected -= OnDisconnect;
            _connection.Received -= OnReceieved;
        }


        private void OnMessageReceived(ITcpConnection connection, IMessage message)
        {
            HandleError(message.Data);
        }

        private void HandleError(IDictionary<string, object> data)
        {
            var error = data.GetOrDefault(k.error, ErrorCodes.NoError);
            if (error == ErrorCodes.NoError)
                return;

            if (error == ErrorCodes.AccountHasBeenDisconnected)
            {
                _log.StatusMessage("already logged in. restarting connect cycle.");
                OnLoginStateChanged(LoginState.AlreadyLoggedIn);
                return;
            }

            if (error == ErrorCodes.NoSuchUser)
            {
                if (_defaultUserTest) return; // don't react this
                _log.StatusError("user/password pair doesn't exist");
               
                OnLoginStateChanged(LoginState.NoSuchUser);
               
                return;
            }

            OnError(error);
        }

        private void OnReceieved(string data)
        {
            _log.Log($"in: {data}");
        }

        protected virtual void OnLoginStateChanged(LoginState newState)
        {
            if (LoginState == newState) return;
            LoginState = newState;

            if (newState == LoginState.AlreadyLoggedIn)
            {
                _log.Log("retry connect");
                Disconnect();
                Thread.Sleep(1000);
                TryConnectToServer(); //retry
            }

            LoginStateChanged?.Invoke(newState);
        }


        private Task<IMessage> LoginAsync(AdminCreds creds)
        {
            if (!Connected || creds == null) { throw new Exception("Wtf? _creds is null"); }

            _log.Log($">>>> trying email:[{creds.Email}] pass:[{creds.Password}]");

            var m = new MessageBuilder().SetCommand(Commands.SignIn)
                .SetData("email",creds.Email)
                .SetData("password",creds.PasswordAsSha1)
                .SetData("client", 0)
                .SetData("hash", "58de48bc09d1eb8fa4434972d6b28cf36053ad3b")
                .Build();

            return _connection.SendAsync(m);
        }


        private void LoginResult(IDictionary<string, object> data)
        {
            if (data.ContainsKey(k.error))
            {
                var errorNumber = data[k.error];
                OnError((ErrorCodes) errorNumber);
                return;
            }

            //success
            _adminCreds.SaveToFile();
            _log.Log("creds saved");
            _log.StatusMessage($"Logged in as {_adminCreds.Email}");
            OnLoginStateChanged(LoginState.Success);
        }


        protected virtual void OnError(ErrorCodes errorCode)
        {
            if (_defaultUserTest) return;
            var errorName = Enum.GetName(typeof(ErrorCodes), errorCode);
            _log.StatusError($"error: {errorName}");

            Error?.Invoke(errorCode);
        }


        //
        // js promise style generic message sender
        //
        public Task SendMessageAsync(IMessage message, Action<IDictionary<string, object>> success, Action<IDictionary<string, object>> failure)
        {
            _log.Log($"out: {message.ToString()}");

            return _connection.SendAsync(message)
                .ContinueWith(t =>
                {
                    var data = t.Result.Data;
                    if (data.ContainsKey(k.error)) { failure(data); }
                    else { success(data); }
                });
        }

        public void StartConnectionSequence()
        {
            _creds = _adminCreds.Duplicate();
            TryConnectToServer();
            _log.Log("connect seq is working");
        }

        private async void TryConnectToServer()
        {
            var loginSuccessWithDefaultAccount = false;
            await Task.Run(() =>
                {
                    _log.Log("-connect started");
                    var isconnected = Connect();
                    _log.Log($"connect result:{isconnected}");
                    if (isconnected) _log.StatusMessage("Connected. Processing account credentials.");
                    _log.Log("-connect returned");

                })
                .ContinueWith(t =>
                {
                    if (!Connected)
                    {
                        _log.StatusError("Unable to connect.");
                        return;
                    }
                    _log.Log("-acc test started");
                    loginSuccessWithDefaultAccount = TestDefaultAccount();
                    if (loginSuccessWithDefaultAccount) _log.StatusMessage("Default account logged in.");
                    Thread.Sleep(1000);
                    _log.Log("-acc test exited");

                })
                .ContinueWith(t2 =>
                {
                    if (!Connected) return;
                    _log.Log("-login with supplied creds started");
                    if (!loginSuccessWithDefaultAccount)
                    {
                        Thread.Sleep(1000);
                        var signedIn = Login(_creds);
                        _log.Log($"sign in success:{signedIn}");
                    }
                    _creds = null;  
                    _log.Log("-login with supplied creds exited");
                });

            
            _log.Log("connect sequence done.");
        }

        public void ShutDownConnectedServer()
        {
            if (!Connected || LoginState != LoginState.Success)
            {
                _log.StatusError("You must be logged in with an admin account.");
                return;
            }


            var result = MessageBox.Show("You are about to send a shutdown command to the connected server.\n\nAre you sure?","WARNING!",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.No) return;

            var info = new Dictionary<string, object>()
            {
                {k.date, DateTime.Now.AddSeconds(10)},
                {k.message, "Server is closing."}
            };

            var m = new MessageBuilder().SetCommand(Commands.ServerShutDown)
                .WithData(info)
                .Build();
            SendMessageAsync(m,ServerShutdownSuccess,ServerShutdownFailure);
        }

        private void ServerShutdownFailure(IDictionary<string, object> obj)
        {
            _log.StatusMessage("Failed to send server shutdown.");
        }

        private void ServerShutdownSuccess(IDictionary<string, object> obj)
        {
            _log.StatusMessage("Server shutdown submitted successfully.");
        }
    }
}

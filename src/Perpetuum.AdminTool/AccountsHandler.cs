using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Perpetuum.AdminTool
{
    public class AccountsHandler
    {
        public event Action<IEnumerable<AccountInfo>> AccountInfosDisplay;
        public event Action OwnerPasswordSet;
        private readonly List<AccountInfo> _accountInfos;
        public AccountInfo DataSource { get; set; }
        public AccountInfo SelectedInfo { get; set; }
        public AccountInfo FreshAccount { get; set; }
        private LogHandler _log;
        private readonly IAccountInfoFactory _accountInfoFactory;
        private NetworkHandler _networkHandler;

        public AccountsHandler(LogHandler logHandler,IAccountInfoFactory accountInfoFactory, NetworkHandler networkHandler)
        {
            _log = logHandler;
            _accountInfoFactory = accountInfoFactory;
            _networkHandler = networkHandler;
            _accountInfos = new List<AccountInfo>();
        }

        private AccountEditorMode _editorMode;

        public AccountEditorMode EditorMode
        {
            get => _editorMode;
            set
            {
                if (_editorMode == value) return;
                if (value == AccountEditorMode.unknown) return;
                if (value == AccountEditorMode.edit)
                {
                    DataSource = SelectedInfo;
                    _editorMode = value;
                }
                if (value == AccountEditorMode.create)
                {
                    FreshAccount = _accountInfoFactory.CreateEmpty();
                    DataSource = FreshAccount;
                    _editorMode = value;
                }
            }
        }

        public void Reset()
        {
            _accountInfos.Clear();
            DataSource = null;
            SelectedInfo = null;
            FreshAccount = null;
            _editorMode = AccountEditorMode.unknown;
        }

        private void ProcessAccountDictinaries(IList<Dictionary<string, object>> accountDicts)
        {
            _accountInfos.Clear();
            _accountInfos.Capacity = accountDicts.Count;

            foreach (var dict in accountDicts)
            {
                var characters = dict.GetOrDefault<Dictionary<string,object>>("characters")?.Select(kvp => new CharacterInfo((Dictionary<string,object>)kvp.Value));
                var accountInfo = _accountInfoFactory.Create((Dictionary<string, object>)dict["account"],characters);
                _accountInfos.Add(accountInfo);
            }

            FilterAccounts();
        }

        private void ProcessSingleAccountDictionary(Dictionary<string, object> data)
        {
            var accountId = data.GetOrDefault<int>(k.accountID);

            var ai = TryRemoveFromRam(accountId);

            var newAccountInfo = _accountInfoFactory.Create(data,ai?.Characters);
            _accountInfos.Add(newAccountInfo);
            FilterAccounts();
        }

        private AccountInfo TryRemoveFromRam(int accountId)
        {
            var ai = _accountInfos.FirstOrDefault(a => a.Id == accountId);
            if (ai != null)
            {
                _accountInfos.Remove(ai);
            }

            return ai;
        }

        private void ProcessAccountDeletedDictionary(Dictionary<string, object> data)
        {
            var accountId = data.GetOrDefault<int>(k.accountID);
            TryRemoveFromRam(accountId);
            FilterAccounts();
        }



        private void FilterAccounts()
        {
            var accountsToDisplay = _filter.IsNullOrEmpty() ? _accountInfos : _accountInfos.Where(a => a.Filter(_filter));
            AccountInfosDisplay(accountsToDisplay.OrderBy(a => a.Email));
        }

        private string _filter = "";

        public void HandleFilterChange(string filter)
        {
            if (_filter == filter) return;
            _filter = filter;
            FilterAccounts();
        }

        public void HandleSelectedInfoChange(AccountInfo accountInfo)
        {
            SelectedInfo = accountInfo;
            if (EditorMode == AccountEditorMode.edit) { DataSource = SelectedInfo; }
        }

        private Dictionary<string, object> GetAccountDict(IDictionary<string,object> data)
        {
            var d = data.Where(p => p.Key.StartsWith(k.account)).Select(p => p.Value).Cast<Dictionary<string, object>>().FirstOrDefault();
            return d;
        }



        // commands ---------------

        public void SendAccountList()
        {
            var m = new MessageBuilder().SetCommand(Commands.GetAccountsWithCharacters)
                .WithData(new Dictionary<string, object>())
                .Build();
            _networkHandler.SendMessageAsync(m, AccountListSuccess, AccountListFailure);
        }


       

        public void SendAccountGet()
        {
            var m = new MessageBuilder().SetCommand(Commands.AccountGet)
                .SetData(k.accountID, SelectedInfo.Id)
                .Build();
            _networkHandler.SendMessageAsync(m, AccountGetSuccess, AccountGetFailure);
        }


        public void SendUpdateWithSelected()
        {
            var pass = SelectedInfo.Password == AccountInfo.DEFAULT_PASSWORD_VALUE ? null : SelectedInfo.Password.ToSha1();

            var m = new MessageBuilder().SetCommand(Commands.AccountUpdate)
                .SetData(k.accountID, SelectedInfo.Id)
                .SetData(k.email, SelectedInfo.Email)
                .SetData(k.accessLevel, (int) SelectedInfo.AccessLevel)
                .SetData(k.password, pass)
                .Build();
            _networkHandler.SendMessageAsync(m, AccountUpdateSuccess, AccountUpdateFailure);
        }


        public void SendAccountCreate()
        {
            var m = new MessageBuilder().SetCommand(Commands.AccountCreate)
                .SetData(k.email, FreshAccount.Email)
                .SetData(k.accessLevel, (int) FreshAccount.AccessLevel)
                .SetData(k.password, FreshAccount.Password.ToSha1())
                .Build();
            _networkHandler.SendMessageAsync(m, AccountCreateSuccess, AccountCreateFailure);
        }


        public void SendChangeSessionPassword(string password)
        {
            var m = new MessageBuilder().SetCommand(Commands.ChangeSessionPassword)
                .SetData(k.password, password)
                .Build();
            _networkHandler.SendMessageAsync(m, AdminPassChangeSuccess, AdminPassChangeFailure);
        }


        public void SendBanWithSelected(TimeSpan banLength, string banNote = "")
        {
            var m = new MessageBuilder().SetCommand(Commands.AccountBan)
                .SetData(k.accountID, SelectedInfo.Id)
                .SetData(k.banLength, (int)banLength.TotalSeconds)
                .SetData(k.banNote, banNote)
                .Build();
            _networkHandler.SendMessageAsync(m, BanSuccess, BanFailure);

        }

        public void SendUnBanWithSelected()
        {
            var m = new MessageBuilder().SetCommand(Commands.AccountUnban)
                .SetData(k.accountID, SelectedInfo.Id)
                .Build();
            _networkHandler.SendMessageAsync(m, UnbanSuccess, UnbanFailure);

        }

        public void SendAccountDelete()
        {
            var m = new MessageBuilder().SetCommand(Commands.AccountDelete)
                .SetData(k.accountID, SelectedInfo.Id)
                .Build();
            _networkHandler.SendMessageAsync(m, DeleteSuccess, DeleteFailure);
        }

        private void DeleteFailure(IDictionary<string, object> obj)
        {
            _log.Log("wtf delete?");
        }

        private void DeleteSuccess(IDictionary<string, object> data)
        {
            var d = GetAccountDict(data);
            var accountId = d.GetOrDefault<int>(k.accountID);
            _log.StatusMessage($"account deleted. id:{accountId}");

            if (SelectedInfo.Id == accountId) SelectedInfo = null; //reset selected

            ProcessAccountDeletedDictionary(d);
        }


        private void UnbanSuccess(IDictionary<string, object> data)
        {
            var d = GetAccountDict(data);
            var accountId = d.GetOrDefault<int>(k.accountID);
            _log.StatusMessage($"account unbanned. id:{accountId}");
            ProcessSingleAccountDictionary(d);
        }

        private void UnbanFailure(IDictionary<string, object> obj)
        {
            _log.Log("wtf");
        }

        private void BanSuccess(IDictionary<string, object> data)
        {
            var d = GetAccountDict(data);
            var accountId = d.GetOrDefault<int>(k.accountID);
            _log.StatusMessage($"account banned. id:{accountId}");
            ProcessSingleAccountDictionary(d);

        }

        private void BanFailure(IDictionary<string, object> data)
        {
            _log.Log("wtf");
        }


        private void AccountListSuccess(IDictionary<string, object> data)
        {
            var accountsRaw = data.Where(p => p.Key.StartsWith("a")).Select(p => p.Value).Cast<Dictionary<string, object>>().ToList();
            _log.StatusMessage($"accounts: {accountsRaw.Count}");
            ProcessAccountDictinaries(accountsRaw);
        }

        private void AccountListFailure(IDictionary<string, object> data)
        {
            var error = data.GetValue<ErrorCodes>(k.error);
            _log.Log("account list: error occured: " + error);
        }


        private void AccountGetSuccess(IDictionary<string, object> data)
        {
            var d = GetAccountDict(data);
            var accountId = d.GetOrDefault<int>(k.accountID);
            _log.StatusMessage($"account loaded. id:{accountId}");
            ProcessSingleAccountDictionary(d);
        }

        private void AccountGetFailure(IDictionary<string, object> data)
        {
            _log.Log("wtf");
        }


        private void AccountUpdateSuccess(IDictionary<string, object> data)
        {
            var d = GetAccountDict(data);
            var accountId = d.GetOrDefault<int>(k.accountID);
            _log.StatusMessage($"account updated. id:{accountId}");
            ProcessSingleAccountDictionary(d);
        }

        private void AccountUpdateFailure(IDictionary<string, object> data)
        {
            AccountGetFailure(data);
        }


        private void AccountCreateSuccess(IDictionary<string, object> data)
        {
            var d = GetAccountDict(data);
            var accountId = d.GetOrDefault<int>(k.accountID);
            _log.StatusMessage($"account created. id:{accountId}");
            ProcessSingleAccountDictionary(d);
        }

        private void AccountCreateFailure(IDictionary<string, object> data)
        {
            _log.Log("wtf");
        }


        private void AdminPassChangeSuccess(IDictionary<string, object> obj)
        {
            _log.Log("Owner password set successfully.");
            _log.StatusMessage("Owner password set successfully.");
            Thread.Sleep(2000); // just to display the message
            OwnerPasswordSet?.Invoke();
        }

        private void AdminPassChangeFailure(IDictionary<string, object> obj)
        {
            _log.Log("wtf? adminpasschange");
        }


      
    }
}

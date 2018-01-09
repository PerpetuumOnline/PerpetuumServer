using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Perpetuum.AdminTool
{
    
    public partial class AccountsPage : UserControl
    {
        public event Action<string> FilterChanged;
        public event Action<AccountInfo> SelectionChanged;

        // ReSharper disable once MemberCanBePrivate.Global
        public ObservableCollection<AccountInfo> Items { get; set; } //needed as public for wpf

        private static readonly TimeSpan _defaultBanlength = TimeSpan.FromMinutes(2);

        private AccountEditControl _accountEditControl;
        private AccountCreateControl _accountCreateControl;
        private AccountsHandler _accountsHandler;
        private LogHandler _log;
        private AdminCreds _adminCreds;

        //
        public AccountsPage()
        {
            InitializeComponent();
        }


        public void Init(LogHandler logHandler, AccountEditControl accountEditControl, AccountCreateControl accountCreateControl, AccountsHandler accountsHandler, AdminCreds adminCreds)
        {
            _log = logHandler;
            _accountsHandler = accountsHandler;
            _accountEditControl = accountEditControl;
            _accountCreateControl = accountCreateControl;
            _adminCreds = adminCreds ;

            Items = new ObservableCollection<AccountInfo>();
            accountListView.ItemsSource = Items;
            banLengthCombo.FillComboBoxForBanLength();
        }

        public void Reset()
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(ApplyReset));
        }

        private void ApplyReset()
        {
            Items.Clear();
        }

        private void GetAccountList_Click(object sender, RoutedEventArgs e)
        {
            _accountsHandler.SendAccountList();
            _accountsHandler.EditorMode = AccountEditorMode.edit;
            SetEditorVisualState();
        }

        public void DisplayAccounts(IEnumerable<AccountInfo> accountInfos)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal,new Action<IEnumerable<AccountInfo>>(PopulateControl), accountInfos);
        }

        private void PopulateControl(IEnumerable<AccountInfo> accountInfos)
        {
            Items.Clear();
            Items.AddMany(accountInfos);

            if (_accountsHandler.EditorMode == AccountEditorMode.edit)
            {
                accountPropertiesRoot.DataContext = null; //needed to refresh the current gui
                accountPropertiesRoot.DataContext = _accountsHandler.SelectedInfo;

                if (_accountsHandler.SelectedInfo != null)
                {
                    var sidx = GetIndexOf(_accountsHandler.SelectedInfo);
                    if (sidx >= 0) accountListView.SelectedIndex = sidx;
                }
            }
        }

        private int GetIndexOf(AccountInfo accountInfo)
        {
            var idx = 0;
            foreach (var item in Items)
            {
                if (item.Id == accountInfo.Id) return idx;
                idx++;
            }
            return -1;
        }


        private void FilterBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb == null) return;
            var filter = tb.Text;
            FilterChanged?.Invoke(filter);
        }

        private void AccountsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // handle selection change anyway
            var lv = sender as ListView;
            var accountInfo = lv?.SelectedItem as AccountInfo;
            if (accountInfo == null) return;
            SelectionChanged?.Invoke(accountInfo);

            banNoteBox.Text = "";
            unbanButton.IsEnabled = accountInfo.IsBanned;
            banStack.IsEnabled = !accountInfo.IsBanned;

             

            //switch editor mode if needed
            if (_accountsHandler.EditorMode == AccountEditorMode.create) return;

            if (_accountsHandler.EditorMode != AccountEditorMode.edit)
            {
                _accountsHandler.EditorMode = AccountEditorMode.edit;
                SetEditorVisualState();
            }
            accountPropertiesRoot.DataContext = _accountsHandler.DataSource;
            
        }


        private void CreateAccount_Click(object sender, RoutedEventArgs e)
        {
            _accountsHandler.EditorMode = AccountEditorMode.create;
            SetEditorVisualState();
        }

        public void CancelAccountCreate()
        {
            _accountsHandler.EditorMode = AccountEditorMode.edit;
            SetEditorVisualState();
        }

        private void SetEditorVisualState()
        {
            //Trace.WriteLine("set editor visual");
            var editModeEnabled = _accountsHandler.EditorMode == AccountEditorMode.edit;

            accountListView.Background = editModeEnabled ? new SolidColorBrush(Color.FromArgb(0xff,0xBB,0xCC,0xDD )) : null;
            accountListView.IsEnabled = editModeEnabled;
            buttonStack.IsEnabled = editModeEnabled;

            if (editModeEnabled)
            {
                //Trace.WriteLine("edit mode");
                accountPropertiesRoot.Children.Clear();
                accountPropertiesRoot.Children.Add(_accountEditControl);
            }
            else
            {
                //create mode
                //Trace.WriteLine("create mode");
                accountPropertiesRoot.Children.Clear();
                accountPropertiesRoot.Children.Add(_accountCreateControl);
            }
            accountPropertiesRoot.DataContext = _accountsHandler.DataSource;
        }

        private void Ban_Click(object sender, RoutedEventArgs e)
        {
            if (!IsAnyAccountSelected()) return;

            var banLength = ((KeyValuePair<TimeSpan, string>?) banLengthCombo.SelectedItem)?.Key ?? _defaultBanlength;

            if (!banLengthCombo.Text.IsNullOrEmpty() && banLength == _defaultBanlength)
            {
                // some text was typed into the combobox
                if (int.TryParse(banLengthCombo.Text, out int parsed))
                {
                    //correct int
                    banLength = TimeSpan.FromSeconds(parsed);
                }
            }

            var banNote = banNoteBox.Text.IsNullOrEmpty() ? "" : banNoteBox.Text;
            Trace.WriteLine($"banLength:{banLength} banNote:{banNote}");

            //ban selected
            _accountsHandler.SendBanWithSelected(banLength, banNote);
        }

        private void Unban_Click(object sender, RoutedEventArgs e)
        {
            if (!IsAnyAccountSelected()) return;

            if (!_accountsHandler.SelectedInfo.IsBanned)
            {
                _log.StatusMessage("The selected account is not banned");
                return;
            }

            _accountsHandler.SendUnBanWithSelected();
        }

        private void Destroy_Click(object sender, RoutedEventArgs e)
        {
            if (!IsAnyAccountSelected()) return;
            if (_accountsHandler.SelectedInfo.Email == _adminCreds.Email)
            {
                _log.StatusError("You cannot delete yourself while logged in.");
                return;
            }

            var boxText = $"This operation will permanently delete the selected account.\n{_accountsHandler.SelectedInfo.Email}";
            const string caption = "Are you sure?";
            const MessageBoxButton button = MessageBoxButton.YesNo;
            const MessageBoxImage icon = MessageBoxImage.Warning;
            const MessageBoxResult defaultResult = MessageBoxResult.No;
            const MessageBoxOptions options = MessageBoxOptions.DefaultDesktopOnly;

            var result = MessageBox.Show(boxText, caption, button, icon, defaultResult, options);

            if (result == MessageBoxResult.Yes)
            {
                _accountsHandler.SendAccountDelete();
            }
        }

        private bool IsAnyAccountSelected()
        {
            if (_accountsHandler.SelectedInfo == null)
            {
                _log.StatusError("Select an account first!");
                return false;
            }
            return true;
        }
    }
}

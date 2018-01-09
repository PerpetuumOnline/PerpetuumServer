using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;


namespace Perpetuum.AdminTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        
        private AuthPage _authPage;
        private AccountsHandler _accountsHandler;
        private AccountsPage _accountsPage;
        private ServerInfoPage _serverInfoPage;
        private NetworkHandler _networkHandler;
        private LogHandler _log;
        private LocalServerPage _localServerPage;
        

        private List<UserControl> _tabItems = new List<UserControl>();


        public MainWindow()
        {
            InitializeComponent();
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(AllChildrenLoaded));
        }

        // all controls loaded event
        private void AllChildrenLoaded()
        {
            _authPage.ResetAll();
            _localServerPage.Setup();
            SetEnableAllTabs(false);
            SetEnabledTabByName("authTab",true);
            SetEnabledTabByName("localServerTab",true);
            Trace.WriteLine("All children loaded");
            if (!_localServerPage.TryRunServer())
            {
                _log.StatusMessage("No local server started");
            }
        }

        public void Init(LogHandler logHandler,    AccountsHandler accountsHandler, NetworkHandler networkHandler, AuthPage authPage, ServerInfoPage serverInfoPage, AccountsPage accountsPage, LocalServerPage localSrvPage)
        {
            Trace.WriteLine("MainWindow init");
            // %%% try minimizing it!
            // local instances to access the pages individually
            _localServerPage = localSrvPage;
            _authPage = authPage;
            _serverInfoPage = serverInfoPage;
            _accountsPage = accountsPage;

            // add them to a list so we can handle them as set of controls
            _tabItems.Add(localSrvPage);
            _tabItems.Add(authPage);
            _tabItems.Add(accountsPage);
            _tabItems.Add(serverInfoPage);

            _networkHandler = networkHandler;
            _log = logHandler;
            _accountsHandler = accountsHandler;
            
        }

        public void SessionOnLoginStateChanged(LoginState loginState)
        {
            //Trace.WriteLine("login state changed: " + loginState);

            if (loginState == LoginState.Success)
            {
                _authPage.SetConnectionConfigForm(false); // disable form input controls

                if (_networkHandler.IsDefaultUser)
                {
                    //must change password
                    _authPage.SetDefaultPasswordText();
                    SetOwnerChangePassVisibility(true);
                    _log.StatusMessage("Please supply a new password the default user ADMIN!");
                    _accountsHandler.OwnerPasswordSet += () =>
                    {
                        // successful default password change
                        _authPage.OwnerPasswordChanged();
                        SetEnableAllTabs(true);
                        FocusTab(serverInfoTab); // initially take the user to server info
                        _serverInfoPage.SendServerInfoGet();
                    };
                    
                }
                else
                {
                    // default case = let the user select the next steps = unlock interface
                    SetEnableAllTabs(true);
                    _authPage.SetBackgroundColorsToDefault();
                }
            }
            else
            {

                FocusTab(authTab);
                _authPage.ResetAll();
                SetEnableAllTabs(false);
                SetEnabledTabByName("authTab", true);
                SetEnabledTabByName("localServerTab",true);

                if (loginState == LoginState.NoSuchUser)
                {
                    // some warning gui things
                    _authPage.ReactToUnsuccessfulLogin();
                }

                 

                if (loginState == LoginState.Disconnected)
                {
                    _log.Log("Disconnected.");
                   
                }
            }

            _authPage.SetTitleByLoginState();
            _accountsHandler.Reset();
            _accountsPage.Reset();
            
        }

        




        // $$$ ez az authPage-re menjen
        public void SetOwnerChangePassVisibility(bool visible)
        {
            SetControlVisibility(visible, _authPage.ownerPassChangeRoot);
        }
        
        private void SetControlVisibility(bool visible, FrameworkElement control)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal,new Action<bool,FrameworkElement>(ApplyControlVisibility), visible, control);
        }

        private void ApplyControlVisibility(bool visible, FrameworkElement control)
        {
            control.SetVisible(visible);
        }


        public void HandleConnectionStateChange(bool connected)
        {
            // nothing atm
        }

       

        #region Main Menu


        private void MenuChangeMyPassword_Click(object sender,RoutedEventArgs e)
        {
            if (_networkHandler.LoginState != LoginState.Success)
            {
                _log.StatusError("You must log in first.");
                return;
            }

            FocusTab(authTab);
            _authPage.SetMyPasswordText();
            SetOwnerChangePassVisibility(true);
            _log.StatusMessage("Please define a new password and press submit.");
            _authPage.HighlightOwnerPassFriendly();
            _authPage.SetOwnerPassChangeButtonText(true);
        }

        private void MenuExit_Click(object sender,RoutedEventArgs e)
        {
            Application.Current.Shutdown(0);
        }

        private void MenuDisconnect_Click(object sender,RoutedEventArgs e)
        {
            _log.Log("manual disconnect");
            _networkHandler.Disconnect();
            _authPage.ResetAll();
        }


        private void MenuClearLog_Click(object sender,RoutedEventArgs e)
        {
            _log.Clear();
        }

        #endregion

        #region Tab page handling

         

        // on/off by name
        public void SetEnabledTabByName(string nameOfEnabledTab, bool enable)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal,new Action<string,bool>(ApplyEnabledTabByName),nameOfEnabledTab,enable);
        }

        private void ApplyEnabledTabByName(string nameOfEnabledTab, bool enable)
        {
            //Trace.WriteLine("nofTabs:" + tabControl.Items.Count);
            foreach (var tabThing in tabControl.Items)
            {
                var tabItem = (TabItem)tabThing;
                //Trace.WriteLine("tabname:" + tabItem.Name);
                if (tabItem.Name != nameOfEnabledTab) continue;
                tabItem.IsEnabled = enable;
            }
        }


        // enables/disbles all tabs
        public void SetEnableAllTabs(bool enabled)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal,new Action<bool>(ApplyEnableAllTabs),enabled);
        }

        private void ApplyEnableAllTabs(bool enabled)
        {
            //Trace.WriteLine("nofTabs:" + tabControl.Items.Count);
            foreach (var tabThing in tabControl.Items)
            {
                var tabItem = (TabItem)tabThing;
                //Trace.WriteLine(tabItem.Name);
                tabItem.IsEnabled = enabled;
            }
        }

        //authTab, accountsTab, serverInfoTab
        public void FocusTab(Control control)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal,new Action<Control>(ApplyFocusTab),control);
        }


        private void ApplyFocusTab(Control control)
        {
            tabControl.SelectedItem = control;
        }

        #endregion

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            if (!_localServerPage.IsRunning) return;

            _log.Log("tool close attempt.");
            var result = MessageBox.Show("You are about to close the AdminTool.\nThe local server will be killed and unsaved data might get lost.\n\nAre you sure?","WARNING!",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _log.Log("Killing local server process...");
                _localServerPage.KillServer();
                _log.Log("Server killed.");
            }
            else
            {
                e.Cancel = true;
            }
            
        }
    }
}

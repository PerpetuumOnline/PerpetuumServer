using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Perpetuum.AdminTool
{
    public partial class AuthPage : UserControl
    {
        public AuthPage(){InitializeComponent();}

        private LogHandler _log;
        private AdminCreds _adminCreds;
        private NetworkHandler _networkHandler;
        private AccountsHandler _accountsHandler;
        public void Init(LogHandler logHandler, AdminCreds adminCreds, NetworkHandler networkHandler,AccountsHandler accountsHandler)
        {
            _log = logHandler;
            _adminCreds = adminCreds;
            _networkHandler = networkHandler;
            _accountsHandler = accountsHandler;
        }
 

        #region Gui operations

        public void SetTitleByLoginState()
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal,new Action(ApplyTitleByLoginState));
        }

        private void ApplyTitleByLoginState()
        {
            connectTitle.Text = _networkHandler.LoginState == LoginState.Success ? "CONNECTION ESTABLISHED" : "CONFIGURE LOGIN";
        }


        public void ResetAll()
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal,new Action(Applyreset));
        }

        private void Applyreset()
        {
            ResetBackgroundColors();

            ownerPassChangeRoot.Visibility = Visibility.Hidden;
            SetDefaultPasswordText();
            SetConnectionConfigForm(true);
            SetTitleByLoginState();
            ownerPassword.Password = null;
            passwordBox.Password = null;
        }

        // config form is = ip,port,email
        public void SetConnectionConfigForm(bool enable)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal,new Action<bool>(ApplyConnectionConfigForm),enable);
        }

        private void ApplyConnectionConfigForm(bool enable)
        {
            ipTextBox.IsEnabled = enable;
            portTextBox.IsEnabled = enable;
            emailTextBox.IsEnabled = enable;
            passwordBox.IsEnabled = enable;
            connectBtn.IsEnabled = enable;
            connectBtn.Visibility = enable ? Visibility.Visible : Visibility.Hidden;
            passwordBox.IsEnabled = enable;
            passwordBox.Visibility = enable ? Visibility.Visible : Visibility.Hidden;
            passwordTxtBlk.Visibility = enable ? Visibility.Visible : Visibility.Hidden;
            defPasswordTxtBlk.Visibility = enable ? Visibility.Visible : Visibility.Hidden;
        }

        // inputs form = password and connect button
        public void SetConnectionInputsForm(bool enable)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal,new Action<bool>(ApplyConnectionInputsForm),enable);
        }

        private void ApplyConnectionInputsForm(bool enable)
        {
            var v = enable ? Visibility.Visible : Visibility.Hidden;
            passwordBox.IsEnabled = enable;
            connectBtn.IsEnabled = enable;
            passwordBox.Visibility = v;
            passwordTxtBlk.Visibility = v;
            connectBtn.Visibility = v;
            ownerPassChangeRoot.Visibility = v;
            defPasswordTxtBlk.Visibility = v;
        }


        private void ResetBackgroundColors()
        {
            ipTextBox.Background = null;
            portTextBox.Background = null;
            emailTextBox.Background = null;
            passwordBox.Background = null;
            ownerPassword.Background = null;
        }

        public void ReactToUnsuccessfulLogin()
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal,new Action(ApplyUnsuccessfulLogin));

        }

        private void ApplyUnsuccessfulLogin()
        {
            passwordBox.Background = new SolidColorBrush(Colors.OrangeRed);
            emailTextBox.Background = new SolidColorBrush(Colors.OrangeRed);
            connectBtn.IsEnabled = true;
        }


        public void HighlightPassFriendly()
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal,new Action(PassToFriendlyColor));
        }

        private void PassToFriendlyColor()
        {
            passwordBox.Background = new SolidColorBrush(Colors.Yellow);
        }

        public void HighlightOwnerPassFriendly()
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal,new Action(OwnerPassToFriendlyColor));
        }

        private void OwnerPassToFriendlyColor()
        {
            ownerPassword.Background = new SolidColorBrush(Colors.Yellow);
        }


        #endregion

        #region Inputs form interaction


        private void ConnectButton_Click(object sender,RoutedEventArgs e)
        {
            _log.StatusMessage($"Trying to connect to {_adminCreds.Ip}:{_adminCreds.Port} with user: {_adminCreds.Email}");
            ConnectToServer();
        }

        private  void ConnectToServer()
        {
            ResetBackgroundColors();
           
            var invalidControlName = _adminCreds.Validate();
            if (invalidControlName == null)
            {
                // the form validates ok
                // start connecting
                connectBtn.IsEnabled = false;
                _networkHandler.StartConnectionSequence();
            }
            else
            {
                // invalid form
                MarkProblem(invalidControlName);
                _log.StatusError("Check the marked field!");
            }
        }

        private void MarkProblem(string invalidControlName)
        {
            var ic = inputStack.FindName(invalidControlName);
            if (ic is Control c)
            {
                c.Background = new SolidColorBrush(Colors.OrangeRed);
            }
        }
        
        private void LoginControl_KeyUp(object sender,KeyEventArgs e)
        {
            if (e.Key == Key.Enter && _networkHandler.LoginState != LoginState.Success)
            {
                ConnectToServer();
            }
        }

        private void PasswordBox_Changed(object sender,RoutedEventArgs e)
        {
            var pb = sender as PasswordBox;
            if (pb == null)
                return;
            _adminCreds.Password = pb.Password;
        }

        #endregion

        #region Change default password
        

        private void AdminPassword_Changed(object sender,RoutedEventArgs e)
        {
            var pb = sender as PasswordBox;
            if (pb == null)
                return;
            _adminCreds.Password = pb.Password;
        }

        private void AdminPasswordSubmit_Click(object sender,RoutedEventArgs e)
        {
            if (_adminCreds.Password.IsNullOrEmpty() || ownerPassword.Password.Length <= 0)
            {
                _log.Log("password is empty...");
                _log.StatusError("Password must be set!");
                return;
            }

            if (_adminCreds.IsDefaultPassword)
            {
                _log.Log("cheeky bastard!");
                _log.StatusError("ADMIN is the default password hence cannot be set.");
                return;
            }

            _accountsHandler.SendChangeSessionPassword(_adminCreds.PasswordAsSha1);
        }

        

        // acts as a toggle
        public void SetOwnerPassChangeButtonText(bool formVisible)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal,new Action<bool>(ApplyOwnerPassChangeButtonText),formVisible);
        }

        public void ApplyOwnerPassChangeButtonText(bool formVisible)
        {
            if (formVisible)
            {
                ownerPassChangeRoot.Visibility = Visibility.Visible;

            }
            else
            {
                ownerPassChangeRoot.Visibility = Visibility.Hidden;

            }
        }


        public void SetMyPasswordText()
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal,new Action(ApplyMyPasswordText));
        }

        public void SetDefaultPasswordText()
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal,new Action(ApplyDefaultPasswordText));
        }

        public void SetBackgroundColorsToDefault()
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(ResetBackgroundColors));
        }


        public void ApplyMyPasswordText()
        {
            ownerPassTitle.Text = "MY PASSWORD";
            ownerPassMessage.Text = $"Set password for the currently logged in account.\n{_adminCreds.Email}";
            cancelOwnerPassChgBtn.Visibility = Visibility.Visible;
        }

        public void ApplyDefaultPasswordText()
        {
            ownerPassTitle.Text = "CHANGE PASSWORD";
            ownerPassMessage.Text = "It seems like you are using the default password.\nFor security reasons it is strongly suggested to set a new password.";
            cancelOwnerPassChgBtn.Visibility = Visibility.Hidden;
        }



        public void OwnerPasswordChanged()
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal,new Action(ReactOwnerPasswordChanged));

        }

        private void ReactOwnerPasswordChanged()
        {
            ResetBackgroundColors();
            ownerPassword.Clear();
            ownerPassChangeRoot.Visibility = Visibility.Hidden;
        }



        private void AdminPasswordCancel_Click(object sender,RoutedEventArgs e)
        {
            // cancel owner pass change
            SetOwnerPassChangeButtonText(false);
            _log.StatusMessage("Password change cancelled.");
        }

        #endregion
    }
}

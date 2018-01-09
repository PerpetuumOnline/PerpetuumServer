using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Perpetuum.AdminTool
{
    public partial class AccountCreateControl : UserControl, IAccountInfoForm
    {
        private Action _cancelAccountCreate;
        private AccountsHandler _accountsHandler;
        private AccountFormValidator _accountFormValidator;

        public Control PasswordBox => passwordBox;
        public Control EmailBox => emailBox;
        public bool PassWordMustBeChanged { get; } = true;

        public AccountCreateControl()
        {
            InitializeComponent();
        }

        public void Init(AccountFormValidator accountFormValidator, AccountsHandler accountsHandler, Action cancelAccountCreate)
        {
            _cancelAccountCreate = cancelAccountCreate;
            _accountFormValidator = accountFormValidator;
            _accountsHandler = accountsHandler;
            cmbAccessLevels.FillComboBoxWithAccessLevel();
        }


        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (_accountFormValidator.ValidateForm(this, _accountsHandler.FreshAccount)) { _accountsHandler.SendAccountCreate(); }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            _cancelAccountCreate();
        }

        private void Pass_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb == null) return;
            if (tb.Text == AccountInfo.DEFAULT_PASSWORD_VALUE) { tb.Text = ""; }
        }

        private void Pass_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb == null) return;
            if (tb.Text.IsNullOrEmpty()) { tb.Text = AccountInfo.DEFAULT_PASSWORD_VALUE; }
        }
    }
}

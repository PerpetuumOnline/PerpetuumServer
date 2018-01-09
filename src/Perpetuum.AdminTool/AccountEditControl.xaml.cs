using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Perpetuum.AdminTool
{
    public interface IAccountInfoForm
    {
        Control PasswordBox { get; }
        Control EmailBox { get; }
        bool PassWordMustBeChanged { get; }
    }


    public partial class AccountEditControl : UserControl, IAccountInfoForm
    {
        
        private  AccountsHandler _accountsHandler;
        private AccountFormValidator _accountFormValidator;

        public Control PasswordBox => passwordBox;
        public Control EmailBox => emailBox;
        public bool PassWordMustBeChanged { get; } = false;

        public AccountEditControl()
        {
            InitializeComponent();
        }

        public void Init(AccountFormValidator accountFormValidator, AccountsHandler accountsHandler)
        {
            _accountFormValidator = accountFormValidator;
            _accountsHandler = accountsHandler;
            cmbAccessLevels.FillComboBoxWithAccessLevel();
        }


        private void AccountEditUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (_accountsHandler.SelectedInfo == null) return;
            if (_accountFormValidator.ValidateForm(this, _accountsHandler.SelectedInfo)) { _accountsHandler.SendUpdateWithSelected(); }
        }

        private void AccountEditRevert_Click(object sender, RoutedEventArgs e)
        {
            if (_accountsHandler.SelectedInfo == null) return;
            _accountsHandler.SendAccountGet();
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

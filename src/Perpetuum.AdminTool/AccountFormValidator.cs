using System.Windows.Controls;
using System.Windows.Media;

namespace Perpetuum.AdminTool
{
    public class AccountFormValidator
    {
        private LogHandler _log;

        public void Init(LogHandler logHandler)
        {
            _log = logHandler;
        }


        public bool ValidateForm(IAccountInfoForm control, AccountInfo accountInfo)
        {
            ResetBackgroundColors(control);
            var problem = ValidateComponents(control, accountInfo);
            if (problem != null)
            {
                MarkProblem(problem, control);
                return false;
            }
            return true;
        }


        private string ValidateComponents(IAccountInfoForm control, AccountInfo accountInfo)
        {
            if (!ValidateEmail(accountInfo)) return "emailBox";
            if (control.PassWordMustBeChanged && !ValidatePassword(accountInfo)) return "passwordBox";
            return null;
        }


        private bool ValidateEmail(AccountInfo accountInfo)
        {
            if (accountInfo.Email.IsNullOrEmpty())
            {
                _log.StatusError("Email must be set!");
                return false;
            }
            return true;
        }

        private bool ValidatePassword(AccountInfo accountInfo)
        {
            if (accountInfo.Password == AccountInfo.DEFAULT_PASSWORD_VALUE)
            {
                _log.StatusError("Password must be set!");
                return false;
            }
            return true;
        }


        private static void MarkProblem(string invalidControlName, IAccountInfoForm controlInterface)
        {
            var control = (Control) controlInterface;

            var ic = control.FindName(invalidControlName);
            if (ic is Control c) { c.Background = new SolidColorBrush(Colors.OrangeRed); }
        }

        private static void ResetBackgroundColors(IAccountInfoForm control)
        {
            control.PasswordBox.Background = null;
            control.EmailBox.Background = null;
        }
    }
}

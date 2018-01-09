using System;
using System.Collections.Generic;
using System.Text;
using Perpetuum.Accounting;
using Perpetuum.AdminTool.ViewModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace Perpetuum.AdminTool
{
    public class AccountInfo : BaseViewModel
    {
        public const string DEFAULT_PASSWORD_VALUE = "not changed";

        public AccountInfo()
        {
            AccessLevel = AccessLevel.normal;
        }

        public List<CharacterInfo> Characters { get; set; } = new List<CharacterInfo>();
        
        private string _email;
        public string Email { get { return _email; } set { _email = value; OnPropertyChanged(nameof(Email)); } }

        private string _password = DEFAULT_PASSWORD_VALUE;
        public string Password { get { return _password; } set { _password = value; OnPropertyChanged(nameof(Password)); } }

        private AccessLevel _accessLevel;
        public AccessLevel AccessLevel { get { return _accessLevel; } set { _accessLevel = value; OnPropertyChanged(nameof(AccessLevel)); } }

        public int Id { get; set; }
        public AccountState AccountState { get; set; }
        public DateTime BanTime { get; set; }
        public TimeSpan BanLength { get; set; } = TimeSpan.FromSeconds(120);
        public string BanNote { get; set; }
        public bool IsModified { get; set; }

        protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            IsModified = true;
            base.OnPropertyChanged(nameof(IsModified));
            base.OnPropertyChanged(nameof(ListBackgroundBrush));
            base.OnPropertyChanged(propertyName);
        }

        public bool Filter(string filter)
        {
            return SearchString.Contains(filter);
        }

        public static AccessLevel ExtractAccessLevel(Dictionary<string, object> data)
        {
            var accLevelRaw = (AccessLevel) data.GetOrDefault<int>(k.accLevel);

            var accLevel = AccessLevel.normal;
            if (accLevelRaw.HasFlag(AccessLevel.normal)) { accLevel = AccessLevel.normal; }

            if (accLevelRaw.HasFlag(AccessLevel.gameAdmin)) { accLevel = AccessLevel.gameAdmin; }

            if (accLevelRaw.HasFlag(AccessLevel.toolAdmin)) { accLevel = AccessLevel.toolAdmin; }

            return accLevel;
        }

        public string Nicks
        {
            get
            {
                if (Characters.Count == 0)
                    return "no active characters";

                var sb = new StringBuilder();
                for (var i = 0; i < Characters.Count; i++)
                {
                    if (i > 0 && i < Characters.Count)
                    {
                        sb.Append(" | ");
                    }
                    dynamic info = Characters[i];
                    sb.Append(info.Nick.ToString());
                }
                return $"| {sb} |";
            }
        }

        public string AccessLevelDisplay => AccessLevel.ToGui();
        public string StateDisplay => $"[{AccessLevelDisplay}] {BanInfo}";

        public bool IsBanned => AccountState == AccountState.banned;
        public string BanInfo => IsBanned ? " - BANNED" : "";

        public string BanDisplay
        {
            get
            {
                if (!IsBanned)
                    return "";

                var sb = new StringBuilder();
                sb.AppendLine("Account is banned.");
                sb.AppendLine($"for: {NiceSeconds(BanLength)}");
                sb.AppendLine($"at: {BanTime.ToCompact(false)}");

                var left = BanTime + BanLength - DateTime.Now;
                if (left.TotalSeconds > 0)
                {
                    sb.AppendLine($"left: {NiceSeconds(left)}");
                }

                sb.AppendLine();

                if (!BanNote.IsNullOrEmpty())
                {
                    sb.AppendLine(BanNote);
                }

                return sb.ToString();
            }
        }

        private static string NiceSeconds(TimeSpan time)
        {
            return time.TotalHours >= 1.0 ? $"{time.TotalHours:F1} hours" : $"{time.TotalSeconds} time";
        }

        public string SearchString
        {
            get
            {
                var sb = new StringBuilder();
                sb.Append(Email);

                foreach (dynamic info in Characters)
                {
                    sb.Append(info.Nick);
                }

                sb.Append(AccessLevelDisplay);
                sb.Append(Id);
                sb.Append(BanInfo);
                return sb.ToString();
            }
        }

        public SolidColorBrush ListBackgroundBrush
        {
            get
            {
                return IsModified ? new SolidColorBrush(Color.FromArgb(0xA0,0xff,0xe0,0x00)) :  new SolidColorBrush(Color.FromArgb(0,0,0,0));
            }
        }
    }
}


using Perpetuum.Services.Relay;

namespace Perpetuum.AdminTool.ViewModel
{
    // tool gui view
    public class ServerInfoViewModel : BaseViewModel
    {
        public string IsOpenGui => OpenStateToGui;
        public string IsBroadcastGui => BroadcastStateToGui;

        public ServerInfo Info { get; private set; } = ServerInfo.None;

        private string OpenStateToGui => IsOpen ? "The server will allow public registration." : 
                                                  "The server will be private, invite only";

        private string BroadcastStateToGui => IsBroadcast ? "The server will be visible in the servers list." :
            "The server will be hidden and won't appear in the servers list.";

        public void UpdateInfo(ServerInfo info)
        {
            Info = info;
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(Description));
            OnPropertyChanged(nameof(Contact));
            OnPropertyChanged(nameof(IsOpen));
            OnPropertyChanged(nameof(IsBroadcast));
            OnPropertyChanged(nameof(IsOpenGui));
            OnPropertyChanged(nameof(IsBroadcastGui));
        }

        public string Name
        {
            get => Info.Name;
            set {
                Info.Name = value;
                OnPropertyChanged();
            }
        }

        public string Description
        {
            get => Info.Description;
            set
            {
                Info.Description = value;
                OnPropertyChanged();
            }
        }

        public string Contact
        {
            get => Info.Contact;
            set
            {
                Info.Contact = value;
                OnPropertyChanged();
            }
        }

        public bool IsOpen
        {
            get => Info.IsOpen;
            set
            {
                Info.IsOpen = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsOpenGui));
            } 
        }

        public bool IsBroadcast
        {
            get => Info.IsBroadcast;
            set
            {
                Info.IsBroadcast = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsBroadcastGui));
            } 
        }
    }
}

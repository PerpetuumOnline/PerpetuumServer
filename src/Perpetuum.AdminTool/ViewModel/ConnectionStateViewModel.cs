namespace Perpetuum.AdminTool.ViewModel
{
    public class ConnectionStateViewModel : BaseViewModel
    {
        public ConnectionStateViewModel(NetworkHandler networkHandler)
        {
            networkHandler.ConnectionStateChanged += connected =>
            {
                State = connected;
                OnPropertyChanged(nameof(State));
            };
        }

        public bool State { get; set; }
    }
}
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Perpetuum.AdminTool.ViewModel;
using Perpetuum.Services.Relay;

namespace Perpetuum.AdminTool
{
    public partial class ServerInfoPage : UserControl
    {
        public ServerInfoPage()
        {
            InitializeComponent();
        }

        private LogHandler _log;
        private ServerInfoViewModel _serverInfoViewModel;
        private NetworkHandler _networkHandler;

        public void Init(LogHandler logHandler, ServerInfoViewModel serverInfoViewModel, NetworkHandler networkHandler)
        {
            _log = logHandler;
            _serverInfoViewModel = serverInfoViewModel;
            _networkHandler = networkHandler;
        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            nameBox.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
            if (_serverInfoViewModel.Info.Name.IsNullOrEmpty())
            {
                nameBox.Background = new SolidColorBrush(Colors.OrangeRed);
                _log.StatusError("Name field must be set!");
                return;
            }

            if (_serverInfoViewModel.Info.Description.IsNullOrEmpty()) { _serverInfoViewModel.Info.Description = ""; }
            if (_serverInfoViewModel.Info.Contact.IsNullOrEmpty()) { _serverInfoViewModel.Info.Contact = ""; }

            SendServerInfoSet();
        }

        private void SendServerInfoSet()
        {
            var serverInfoData = _serverInfoViewModel.Info.Serialize();
            var m = new MessageBuilder().SetCommand(Commands.ServerInfoSet)
                .WithData(serverInfoData)
                .Build();
            _networkHandler.SendMessageAsync(m, ServerInfoSetSuccess, ServerInfoSetFailure);
        }

        private void ServerInfoSetSuccess(IDictionary<string, object> data)
        {
            UpdateServerInfoFromDictionary(data);
            _log.StatusMessage("Server info submitted successfully.");
        }

        private void ServerInfoSetFailure(IDictionary<string, object> obj)
        {
            _log.Log("wtf?");
        }

        private void GetCurrent_Click(object sender, RoutedEventArgs e)
        {
            SendServerInfoGet();
        }

        public Task SendServerInfoGet()
        {
            var m = new MessageBuilder().SetCommand(Commands.ServerInfoGet).Build();
            return _networkHandler.SendMessageAsync(m, ServerInfoGetSuccess, ServerInfoGetFailure);
        }

        private void ServerInfoGetSuccess(IDictionary<string, object> data)
        {
            UpdateServerInfoFromDictionary(data);
            _log.StatusMessage("Server info retrieved.");
        }

        private void UpdateServerInfoFromDictionary(IDictionary<string, object> data)
        {
            var serverInfo = ServerInfo.Deserialize(data);
            _serverInfoViewModel.UpdateInfo(serverInfo);
        }

        private void ServerInfoGetFailure(IDictionary<string, object> obj)
        {
            _log.Log("wtf?");
        }

        private void ShutDown_Click(object sender, RoutedEventArgs e)
        {


            _networkHandler.ShutDownConnectedServer();


        }
    }
}

using System;

namespace Perpetuum.AdminTool.ViewModel
{
    public class LocalServerStateViewModel : BaseViewModel
    {
        public LocalServerState State{ get; set;}
        public bool IsKillEnabled { get; set; } = false;
        public bool IsRunEnabled { get; set; } = true;
        public string VerboseMessage { get; set; }
        public string ProbeText { get; set; } = "";

        public LocalServerStateViewModel(LocalServerRunner localServerRunner)
        {
            localServerRunner.ServerStateChanged += serverState =>
            {
                // kill is enabled in all states when process is running
                IsKillEnabled = (serverState == LocalServerState.starting) || (serverState == LocalServerState.listening);
                OnPropertyChanged(nameof(IsKillEnabled));

                // run is the opposite 
                IsRunEnabled = !IsKillEnabled;
                OnPropertyChanged(nameof(IsRunEnabled));

                State = serverState;
                OnPropertyChanged(nameof(State));

                VerboseMessage = GetMessageByState();
                OnPropertyChanged(nameof(VerboseMessage));
            };

            localServerRunner.ProbeTextChanged += probeText =>
            {
                ProbeText = probeText;
                OnPropertyChanged(nameof(ProbeText));
            };
        }

        private string GetMessageByState()
        {
            var res = "";
            switch (State)
            {
                case LocalServerState.unknown:
                    res = "";
                    break;
                case LocalServerState.starting:
                    res = "Please wait until the server starts up. This might take a few minutes.";
                    break;
                case LocalServerState.listening:
                    res = "The server is running. You can connect to it using this AdminTool or a Perpetuum client. Every local server is listening to IP address 127.0.0.1. The default port is 17700.\nTo shut down and save all game world data use the Server Shutdown function!";
                    break;
                case LocalServerState.shutdownok:
                    res = "Perpetuum world state is saved. Next time the server will continue from here.";
                    break;
                case LocalServerState.upnperror:
                    res = "An error occured during UPNP configuration. Please check your firewall's and router's UPNP settings! You can also disable the initial UPNP operation in the server config file. Set EnableUpnp=false in Perpetuum.ini!";
                    break;
                case LocalServerState.exitwitherror:
                    res = "The server process exited unexpectedly. If it wasn't planned then check the operating system's event logs and local server logs!";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return res;
        }
    }
}

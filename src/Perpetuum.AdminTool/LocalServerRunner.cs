using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Perpetuum.Network;

namespace Perpetuum.AdminTool
{
    public class LocalServerRunner
    {
        private const string LOCALSERVERINFO = "localserverinfo.json";
        private const string PERPETUUMINIFILE = "Perpetuum.ini";
        private Process _serverProcess;
        private bool _stopProbing;
        private LogHandler _log;
        private LocalServerState _localServerState;
        public LocalServerState State
        {
            get => _localServerState;
            private set
            {
                if (_localServerState == value) return;
                _localServerState = value;
                ServerStateChanged?.Invoke(value);
            }
        }

        private string _probeText = "";

        private string ProbeText
        {
            get => _probeText;
            set
            {
                if(_probeText.Equals(value)) return;
                _probeText = value;
                ProbeTextChanged?.Invoke(value);
            }
        }

        public event Action<LocalServerState> ServerStateChanged;
        public event Action<string> ProbeTextChanged;
        public event Action<string> ServerProcessOnOutData;
        public event Action<string> ServerProcessOnErrorData;

        public void Init(LogHandler logHandler)
        {
            _log = logHandler;
        }

        public void Run(string gameRoot, string pathToExecutable)
        {
            try
            {
                var workingDir = Path.GetDirectoryName(pathToExecutable);
                if (workingDir == null)
                {
                    _log.StatusMessage("Error occured. Check the executable path!");
                    return;
                }

                _serverProcess = new Process()
                {
                    StartInfo = new ProcessStartInfo(pathToExecutable)
                    {
                        UseShellExecute = false,
                        WindowStyle = ProcessWindowStyle.Normal,
                        WorkingDirectory = workingDir,
                        Arguments = gameRoot,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                    }
                };

                // trigger the event -> disables the run button
                this.State = LocalServerState.starting;
                // enble exit event raise
                _serverProcess.EnableRaisingEvents = true;
                // this will fire when the server process exits
                _serverProcess.Exited += OnProcessExited;
                _serverProcess.OutputDataReceived += OnOutDataReceived;
                _serverProcess.ErrorDataReceived += OnErrorDataReceived;

                // summer car
                _serverProcess.Start();
                _serverProcess.BeginOutputReadLine();
                _serverProcess.BeginErrorReadLine();

                // save localserverinfo file
                SaveLocalServerInfo(gameRoot, pathToExecutable);

                var listeningPort = GetPortFromIni(gameRoot);
                if (listeningPort <= 0)
                {
                    _log.StatusError($"Wasn't able to get listening port from {PERPETUUMINIFILE}. No port probe is started.");
                    return;
                }

                //RunNetStatProbe(listeningPort);
                RunSocketProbe(listeningPort);
            }
            catch (Exception ex)
            {
                _log.Log(ex.Message);
                _log.StatusError("Error occured starting the local server. Please check logs and os events!");
            }
        }



        public static bool TryLoadLocalServerInfoFile(out string gameRoot, out string pathToExecutable)
        {
            gameRoot = "";
            pathToExecutable = "";
            if (!File.Exists(LOCALSERVERINFO)) return false;

            var json = File.ReadAllText(LOCALSERVERINFO);
            var lsInfo = JsonConvert.DeserializeAnonymousType(json,new
            {
                GameRoot = "",
                PathToServerExe = "",
            });

            gameRoot = lsInfo.GameRoot;
            pathToExecutable = lsInfo.PathToServerExe;
            return true;
        }


        private static void SaveLocalServerInfo(string gameRoot, string pathToExecutable)
        {
            var json =
            JsonConvert.SerializeObject(
                new
                {
                    GameRoot = gameRoot,
                    PathToServerExe = pathToExecutable
                },Formatting.Indented);

            File.WriteAllText(LOCALSERVERINFO, json);
        }


        private void OnProcessExited(object sender, EventArgs eventArgs)
        {
            var p = (Process) sender;
            _log.Log($"closed with exitcode:{p.ExitCode}");

            var state = LocalServerState.unknown;
            switch (p.ExitCode)
            {
                case 0:
                    _log.StatusMessage("Perpetuum server was shut down properly.");
                    state = LocalServerState.shutdownok;
                    break;

                case 2000:
                    _log.StatusError("UPNP config error occured, server cannot start.");
                    state = LocalServerState.upnperror;
                    break;

                default:
                    _log.StatusError("Error occured, Perpetuum server quit unexpectedly.");
                    state = LocalServerState.exitwitherror;
                    break;
            }

            this.State = state;
            _stopProbing = true;

        }


        private void OnOutDataReceived(object sender,DataReceivedEventArgs e)
        {
            if (e.Data.IsNullOrEmpty()) return;
            ServerProcessOnOutData?.Invoke(e.Data);
        }


        private void OnErrorDataReceived(object sender,DataReceivedEventArgs e)
        {
            if (e.Data.IsNullOrEmpty()) return;
            ServerProcessOnErrorData?.Invoke(e.Data);
        }

        public void Kill()
        {
            Task.Run(() =>
            {
                _log.StatusMessage($"Killing process. Id:{_serverProcess.Id}");
                _serverProcess.Kill(); // this is async, so wait for it
                _serverProcess.WaitForExit();
                _log.StatusMessage($"Process killed successfully.");
                
            }).Wait();
        }

        #region Netstat probe
        // this is one method that works 100%
        // but it's not as slick as the socket version.

        private void RunNetStatProbe(int listeningPort)
        {
            Task.Run(() => {
                ProbePortWithNetStat(listeningPort);
            });
            _log.Log($"Netstat port probe started. Target port: {listeningPort}");
        }


        // standard version
        // no elevation needed - but no exename
        // filter: 17700
        //netstat -ano -p TCP | grep 17700 

        // elevated version
        // the app would need elevation 
        // filter1: Perpetuum.Server.exe
        // filter2: 17700
        //netstat -anbo -p TCP | grep -B 1 Perpetuum 
        private void ProbePortWithNetStat(int listeningPort)
        {
            var count = 0;
            while (true)
            {
                var relayProbe = new Process
                {
                    StartInfo =
                    {
                        FileName = "netstat.exe",
                        Arguments = "-ano -p TCP",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };

                relayProbe.Start();

                var probeResult = relayProbe.StandardOutput.ReadToEnd();
                //Trace.WriteLine(probeResult);
                //_log.Log(probeResult);
                relayProbe.WaitForExit();

                if (probeResult.Contains(listeningPort.ToString()))
                {
                    _log.StatusMessage($"Server is listening to port: {listeningPort}");
                    State = LocalServerState.listening;
                    ProbeText = "";
                    return;
                }

                ProbeText = NextIndicatorText();
                Thread.Sleep(2000);

                if (count++ > 200)
                {
                    _log.StatusError("Timeout. Giving up probing with socket.");
                    return;
                }

            }

        }

        #endregion

        #region Socket probe
        // thid one is used

        private void RunSocketProbe(int listeningPort)
        {
            _stopProbing = false;
            Task.Run(() => {
                ProbePortWithSocket(listeningPort);
            });
            _log.Log($"Socket port probe started. Target port: {listeningPort}");
        }


        private void ProbePortWithSocket(int listeningPort)
        {
            const int sleepMs = 200;
            var count = 0;
            while (true)
            {
                if (_stopProbing)
                {
                    ProbeText = "";
                    break;
                }
                Thread.Sleep(sleepMs);
                ProbeText = NextIndicatorText(); // make it spin!

                if (count++ > 800)
                {
                    _log.StatusError("Timeout. Giving up probing with socket.");
                    return;
                }

                if (count % 4 != 0)continue;

                try
                {
                    var connection = ClientConnection.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"),listeningPort));
                    _log.Log($"Socket port probe connected successfully. Closing probe.");
                    _log.StatusMessage($"Server is listening to port: {listeningPort}");
                    State = LocalServerState.listening;
                    ProbeText = "";
                    return;
                }
                catch (Exception) { }

            }

        }



        #endregion

        private int _indicatorTextIndex = 0;
        private readonly string[] _indicatorPhases = { "|", "/" ,"-", @"\" };
        

        private string NextIndicatorText()
        {
            _indicatorTextIndex++;
            _indicatorTextIndex = _indicatorTextIndex % (_indicatorPhases.Length);
            return _indicatorPhases[_indicatorTextIndex];
        }

        private int GetPortFromIni(string gameRoot)
        {
            var perpetuumIni = Path.Combine(gameRoot, PERPETUUMINIFILE);
            if (!File.Exists(perpetuumIni))
            {
                _log.StatusError($"{PERPETUUMINIFILE} was not found in game root {gameRoot} folder. Check your settings!" );
                return -1;
            }

            var json = File.ReadAllText(perpetuumIni);
            var portData = JsonConvert.DeserializeAnonymousType(json,new
            {
                ListenerPort = -1,
            });

            _log.Log($"Relay port loaded from {PERPETUUMINIFILE}: {portData.ListenerPort}");

            return portData.ListenerPort;
        }


    }
}

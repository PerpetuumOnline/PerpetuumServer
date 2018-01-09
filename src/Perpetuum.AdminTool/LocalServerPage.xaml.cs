using System;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Threading;
using MessageBox = System.Windows.MessageBox;
using MessageBoxOptions = System.Windows.MessageBoxOptions;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using UserControl = System.Windows.Controls.UserControl;

namespace Perpetuum.AdminTool
{
    

    /// <summary>
    /// Interaction logic for LocalServerPage.xaml
    /// </summary>
    public partial class LocalServerPage : UserControl
    {
        private const string EXECUTABLEPATH = "-e"; // path to Perpetuum.Server.exe default: [InstallDir]/Perpetuum.Server.exe
        private const string GAMEROOTFOLDER = "-g"; // the default is [InstallDir]/data  - bin, layers and other folder can be found here
        private const string AUTORUNSERVER = "-a"; // offer running the server immediately after start

        private const string DEFAULTGAMEROOT = "data";
        private const string DEFAULTEXEPATH = "Perpetuum.Server.exe";


        private LogHandler _log;
        private LocalServerRunner _localServerRunner;
        private string ExecutablePath { get; set; }
        private string GameRoot { get; set; }
        public bool AutoRunAfterStart { get; set; }
        public LocalServerState State => _localServerRunner.State;
        public bool IsRunning => _localServerRunner.State == LocalServerState.listening || _localServerRunner.State == LocalServerState.starting;
        public LocalServerPage()
        {
            InitializeComponent();
        }

        public void Init(LogHandler logHandler, LocalServerRunner localServerRunner)
        {
            _log = logHandler;
            _localServerRunner = localServerRunner;
            ExecutablePath = DEFAULTEXEPATH;
            GameRoot = DEFAULTGAMEROOT;
            gameRootPathTxtBox.Text = GameRoot;
            executablePathTxtBox.Text = ExecutablePath;

            _localServerRunner.ServerProcessOnErrorData += HandleErrorData;
            _localServerRunner.ServerProcessOnOutData += HandleOutData;

        }

        private void HandleOutData(string line)
        {
             Dispatcher.BeginInvoke(DispatcherPriority.Normal,new Action<string>(ServerProcessLogAddLine), line);
        }

        private void HandleErrorData(string line)
        {
            Console.Beep();
            Dispatcher.BeginInvoke(DispatcherPriority.Normal,new Action<string>(ServerProcessLogAddLine),line);
        }


        private int _lineCount = 0;
        private void ServerProcessLogAddLine(string line)
        {
            var theText = processLogTextBox.Text;
            _lineCount++;
            if (_lineCount > 200)
            {
                var idx = theText.LastIndexOf('\n');
                theText = theText.Substring(0, idx);
            }
            theText = line + '\n' + theText;
            processLogTextBox.Text = theText;
        }

        private void ServerProcessLogClear()
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal,new Action(ServerProcessLogReset));
        }

        private void ServerProcessLogReset()
        {
            processLogTextBox.Text = "";
            _lineCount = 0;
        }

        public void Setup()
        {
            // the settings file can be overridden with command line arguments
            // so the order is important here
            TryLocalServerFile();
            TryCommandLineArgs();
        }

        private void TryLocalServerFile()
        {
            var gameRoot = DEFAULTGAMEROOT;
            var exePath = DEFAULTEXEPATH;
            if (LocalServerRunner.TryLoadLocalServerInfoFile(out gameRoot,out exePath))
            {
                GameRoot = gameRoot;
                gameRootPathTxtBox.Text = gameRoot;
                ExecutablePath = exePath;
                executablePathTxtBox.Text = exePath;
                _log.Log($"saved local server info was loaded.\ngameroot: [{gameRoot}]\nexepath: [{exePath}]");
                _log.StatusMessage("Local server info loaded from file.");
            }
            else
            {
                _log.Log("no saved local server info was found.");
            }
        }
        
        public void TryCommandLineArgs()
        {
            if (App.CommandLineArgs.Count <= 0) return;

            _log.Log("command line args were detected for local server runner.");
            var args = App.CommandLineArgs;
            if (args.ContainsKey(EXECUTABLEPATH))
            {
                ExecutablePath = args[EXECUTABLEPATH];
                executablePathTxtBox.Text = ExecutablePath;
                _log.Log($"exepath: [{ExecutablePath}]");
                
            }
            if (args.ContainsKey(GAMEROOTFOLDER))
            {
                GameRoot = args[GAMEROOTFOLDER];
                gameRootPathTxtBox.Text = GameRoot;
                _log.Log($"gameroot: [{GameRoot}]");
            }

            if (args.ContainsKey(AUTORUNSERVER))
            {
                AutoRunAfterStart = true;
            }
        }

        

        private void ButtonRunLocal_click(object sender, RoutedEventArgs e)
        {
           StartLocalServer();
        }

        private void StartLocalServer()
        {
            if (ValidateForm())
            {
                ServerProcessLogClear();
                _localServerRunner.Run(GameRoot,ExecutablePath);
                _log.StatusMessage("Local server started");

            }
        }


        private bool ValidateForm()
        {
            ResetBackgroundColors();
            var eCol = new SolidColorBrush(Colors.OrangeRed);
            if (ExecutablePath.IsNullOrEmpty())
            {
                _log.StatusError("Executable path not set.");
                executablePathTxtBox.Background = eCol;
                return false;
            }

            if (!File.Exists(ExecutablePath))
            {
                _log.StatusError("Ececutable not found.");
                executablePathTxtBox.Background = eCol;
                return false;
            }

            if (GameRoot.IsNullOrEmpty())
            {
                _log.StatusError("Gameroot folder not set.");
                gameRootPathTxtBox.Background = eCol;
                return false;
            }

            if (!Directory.Exists(GameRoot))
            {
                _log.StatusMessage("Game root folder not found.");
                gameRootPathTxtBox.Background = eCol;
                return false;
            }

            return true;
        }

        private void ResetBackgroundColors()
        {
            executablePathTxtBox.Background = null;
            gameRootPathTxtBox.Background = null;
        }

        private void ButtonKill_click(object sender,RoutedEventArgs e)
        {
            var boxText = $"This action will immediately terminate the server process.\nBy doing so recent changes might get lost.\nAre you sure?";
            const string caption = "Warning!";
            const MessageBoxButton button = MessageBoxButton.YesNo;
            const MessageBoxImage icon = MessageBoxImage.Warning;
            const MessageBoxResult defaultResult = MessageBoxResult.No;
            const MessageBoxOptions options = MessageBoxOptions.DefaultDesktopOnly;

            var result = MessageBox.Show(boxText,caption,button,icon,defaultResult,options);

            if (result == MessageBoxResult.Yes)
            {
               _localServerRunner.Kill();
            }
        }

        private void BrowseExe_click(object sender,RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Multiselect = false,
                Filter = "Server executable|Perpetuum.Server.exe;", // "Server executable (*.exe)|*.exe;",
                RestoreDirectory = true
            };

            if (ofd.ShowDialog() == true)
            {
                executablePathTxtBox.Text= ofd.FileName;
            }
        }

         
        private void BrowseGameRoot_click(object sender,RoutedEventArgs e)
        {
            var folderName = "";
            var dialog = new FolderBrowserDialog();
            var res = dialog.ShowDialog();
            if (res == DialogResult.OK)
            {
                folderName = dialog.SelectedPath;
            }

            gameRootPathTxtBox.Text = folderName;
        }

        private void ExePath_Changed(object sender,System.Windows.Controls.TextChangedEventArgs e)
        {
            ExecutablePath = (sender as System.Windows.Controls.TextBox).Text;
        }
        
        private void GameRoot_Changed(object sender,System.Windows.Controls.TextChangedEventArgs e)
        {
            GameRoot = (sender as System.Windows.Controls.TextBox).Text;
        }


        public bool TryRunServer()
        {
            if (!AutoRunAfterStart) return false;

            var boxText = "Do you want to start Perpetuum server now?";
            const string caption = "Fresh installation";
            const MessageBoxButton button = MessageBoxButton.YesNo;
            const MessageBoxImage icon = MessageBoxImage.Warning;
            const MessageBoxResult defaultResult = MessageBoxResult.Yes;
            const MessageBoxOptions options = MessageBoxOptions.DefaultDesktopOnly;

            var result = MessageBox.Show(boxText,caption,button,icon,defaultResult,options);

            if (result == MessageBoxResult.Yes)
            {
                StartLocalServer();
                return true;
            }

            return false;
        }

        public void KillServer()
        {
            _localServerRunner.Kill();

        }
    }
}

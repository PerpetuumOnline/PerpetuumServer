using System;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Perpetuum.AdminTool
{
    /// <summary>
    /// Interaction logic for LoggerControl.xaml
    /// </summary>
    public partial class LoggerControl : UserControl
    {
        public LoggerControl()
        {
            InitializeComponent();
        }

        public void HandleLog(string line)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal,new Action<string>(AddLine), line);
        }

        private void AddLine(string line)
        {
            if (!line.EndsWith("\n")) { line += "\n"; }
            logTextBox.Text += line;
        }

        public void Clear()
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal,new Action(ResetText));
        }

        private void ResetText()
        {
            logTextBox.Text = "";
        }
    }
}

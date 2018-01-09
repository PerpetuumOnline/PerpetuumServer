using System;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Perpetuum.AdminTool
{
    /// <summary>
    /// Interaction logic for StatusTextControl.xaml
    /// </summary>
    public partial class StatusTextControl : UserControl
    {
        public StatusTextControl()
        {
            InitializeComponent();
        }

        public void HandleTextChanged(string text)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal,new Action<string>(SetText), text);
        }

        private void SetText(string text)
        {
            statusText.Text = text;
        }

        public void HandleColorChanged(Color color)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal,new Action<Color>(SetColor), color);
        }

        private void SetColor(Color color)
        {
            statusTextGrid.Background = new SolidColorBrush(color);
        }
    }
}

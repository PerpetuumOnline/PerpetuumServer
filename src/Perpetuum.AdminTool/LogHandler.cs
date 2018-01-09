using System.Windows.Media;

namespace Perpetuum.AdminTool
{
    public class LogHandler
    {
        private readonly LoggerControl _logger;
        private readonly StatusTextControl _statusText;

        public LogHandler(LoggerControl logger, StatusTextControl statusText)
        {
            _logger = logger;
            _statusText = statusText;
        }

        public void Log(string text)
        {
            _logger.HandleLog(text);
        }

        public void StatusMessage(string text)
        {
            _statusText.HandleTextChanged(text);
            _statusText.HandleColorChanged(Color.FromArgb(0,0,0,0));
        }

        public void StatusError(string text)
        {
            _statusText.HandleTextChanged(text);
            _statusText.HandleColorChanged(Colors.OrangeRed);
        }

        public void Clear()
        {
            _logger.Clear();
        }
    }
}

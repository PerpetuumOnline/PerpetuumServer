using System;
using System.IO;
using System.Text;
using System.Threading;
using Perpetuum.IO;

namespace Perpetuum.Log.Loggers
{
    public class FileLogger<T> : BufferedLogger<T> where T : ILogEvent
    {
        private readonly ILogEventFormatter<T,string> _formatter;
        private readonly IFileSystem _fileSystem;
        private readonly Func<string> _pathFactory;

        public delegate FileLogger<T> Factory(ILogEventFormatter<T, string> formatter, Func<string> pathFactory);

        public FileLogger(IFileSystem fileSystem, ILogEventFormatter<T, string> formatter, Func<string> pathFactory)
        {
            _formatter = formatter;
            _fileSystem = fileSystem;
            _pathFactory = pathFactory;
        }

        protected override void Flush(T[] logEvents)
        {
            if ( logEvents.Length == 0 )
                return;

            var path = _pathFactory();
            
            var directoryPath = Path.GetDirectoryName(path);
            if (directoryPath == null)
                return;

            _fileSystem.CreateDirectory(directoryPath);

            var builder = new StringBuilder();

            foreach (var logEvent in logEvents)
            {
                builder.AppendLine(_formatter.Format(logEvent));
            }

            while (true)
            {
                try
                {
                    _fileSystem.AppendAllText(path,builder.ToString());
                    return;
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex);
                }

                Thread.Sleep(1000);
            }
        }
    }
}
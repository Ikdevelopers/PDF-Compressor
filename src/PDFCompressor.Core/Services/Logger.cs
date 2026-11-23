using System;
using System.IO;
using System.Text;

namespace PDFCompressor.Core.Services
{
    public static class Logger
    {
        private static readonly object _lock = new();
        private static string _logDirectory = string.Empty;
        private static string _logFilePath = string.Empty;

        static Logger()
        {
            try
            {
                _logDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "PDFCompressor",
                    "logs");
                
                Directory.CreateDirectory(_logDirectory);
                _logFilePath = Path.Combine(_logDirectory, $"app_{DateTime.UtcNow:yyyyMMdd}.log");
            }
            catch
            {
                // Fallback to current directory
                _logDirectory = AppDomain.CurrentDomain.BaseDirectory;
                _logFilePath = Path.Combine(_logDirectory, "pdfcompressor.log");
            }
        }

        public static string LogFilePath => _logFilePath;

        public static void Info(string message) => Write("INFO", message);
        public static void Warn(string message) => Write("WARN", message);
        public static void Error(string message, Exception? ex = null)
        {
            var sb = new StringBuilder(message);
            if (ex != null)
            {
                sb.AppendLine();
                sb.AppendLine($"Exception: {ex.GetType().FullName}: {ex.Message}");
                sb.AppendLine($"StackTrace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    sb.AppendLine($"Inner: {ex.InnerException.GetType().FullName}: {ex.InnerException.Message}");
                }
            }
            Write("ERROR", sb.ToString());
        }

        private static void Write(string level, string message)
        {
            lock (_lock)
            {
                try
                {
                    string entry = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}";
                    File.AppendAllText(_logFilePath, entry + Environment.NewLine, Encoding.UTF8);
                }
                catch
                {
                    // Logging should never crash the main application
                }
            }
        }
    }
}

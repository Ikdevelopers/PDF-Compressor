using System;
using System.Windows.Forms;
using PDFCompressor.Core.Services;

namespace PDFCompressor.App
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            }
            catch { }

            ApplicationConfiguration.Initialize();

            // Global exception handling
            Application.ThreadException += (s, e) =>
            {
                Logger.Error("Unhandled UI Thread Exception", e.Exception);
                MessageBox.Show(
                    $"An unexpected error occurred:\n\n{e.Exception.Message}\n\nDetails have been logged to the application log.",
                    "PDF Compressor Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            };

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                {
                    Logger.Error("Unhandled AppDomain Exception", ex);
                }
            };

            Logger.Info($"Starting PDF Compressor (Process: {(Environment.Is64BitProcess ? "x64" : "x86")}, OS: {Environment.OSVersion})");

            Application.Run(new MainForm());

            Logger.Info("PDF Compressor exited normally.");
        }
    }
}

using STEditor.Services.Interfaces;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows;

namespace STEditor.Services
{
    public class LogService : ILogService
    {
        private readonly string _logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        private string _currentLogFile;
        public string LogFilePath => _currentLogFile;
        public LogService() 
        {
            Directory.CreateDirectory(_logPath);
            _currentLogFile = Path.Combine(_logPath, $"log_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
        }

        public void ShowDialog(string title)
        {
            var result = MessageBox.Show(
                $"A critical error has occurred and the application will be terminated.\n\nOpen logs folder?\n\n{LogFilePath}",
                title,
                MessageBoxButton.YesNo,
                MessageBoxImage.Error);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    Process.Start("explorer.exe", $"/select, \"{_logPath}\"");
                }
                catch (Exception ex)
                {
                    LogError("Failed to open logs folder", ex);
                }
            }

        }
        public void LogError(string message, Exception ex)
        {
            WriteToFile($"<-!!!->[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n{ex}\n");
        }
        public void LogInfo(string message)
        {
            WriteToFile($"<-i->[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n");
        }
        public void LogWarning(string message, Exception ex)
        {
            WriteToFile($"<-?->[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n{ex}\n");
        }
        public void WriteToFile(string logText) 
        {
            try
            {
                using (var writer = new StreamWriter(LogFilePath, true, Encoding.UTF8))
                {
                    writer.Write(logText);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to write to log file: {ex}");
            }
        }   
    }
}

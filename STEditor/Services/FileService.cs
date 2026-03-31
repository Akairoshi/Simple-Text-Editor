using STEditor.Infrastructure;
using STEditor.Services.Interfaces;
using System.IO;
using System.Text;

namespace STEditor.Services
{
    public sealed class FileService : IFileService
    {
        private readonly ILogService _logService;
        public FileService(ILogService logService)
        {
            _logService = logService;

            _logService.LogInfo("FileService initialized");
        }
        public string ReadFile(string filePath)
        {
            _logService.LogInfo($"Read file {filePath}");
            string text = File.ReadAllText(filePath, Encoding.UTF8);
            return text;
        }

        public void WriteToFile(string filePath, string text, Encoding encoding)
        {
            _logService.LogInfo($"Writing to file {filePath}");
            File.WriteAllText(filePath, text, encoding);
        }
    }
}
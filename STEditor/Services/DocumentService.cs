using STEditor.Infrastructure;
using STEditor.Models;
using STEditor.Services.Interfaces;
using System.IO;
using System.Text;
namespace STEditor.Services
{
    class DocumentService : IDocumentService
    {
        private readonly IFileService _fileService;
        private readonly ILogService _logService;

        public DocumentService(IFileService fileService, ILogService logService)
        {
            _logService = logService;
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            
            _logService.LogInfo("DocumentService initialized");
        }

        private void OpenFileCore(DocumentState document, string path)
        {
            _logService.LogInfo($"Opening file: {path}");
            var result = _fileService.ReadFile(path);

            document.Text = result;
            document.FilePath = path;
            document.LastSavedText = document.Text;

            var ext = Path.GetExtension(path)?.ToLowerInvariant();

            document.HighlightKind = ext switch
            {
                ".md" => EditorHighlightKind.Markdown,
                ".json" => EditorHighlightKind.Json,
                ".cs" => EditorHighlightKind.CSharp,
                _ => EditorHighlightKind.None
            };
        }
        public void OpenFile(DocumentState document, string path)
        {
            OpenFileCore(document, path);
        }

        public void OpenFileFromPath(DocumentState document, string path)
        {
            if (!File.Exists(path))
                return;

            OpenFileCore(document, path);
        }

        public void SaveFile(DocumentState document)
        {
            _logService.LogInfo($"Save file: {document.FilePath}");
            _fileService.WriteToFile(document.FilePath, document.Text, document.Encoding);
            document.LastSavedText = document.Text;
        }

        public void SaveFileAs(DocumentState document, string path)
        {
            _logService.LogInfo($"Save file: {path}");
            document.FilePath = path;
            _fileService.WriteToFile(document.FilePath, document.Text, document.Encoding);
            document.LastSavedText = document.Text;
        }

        public void NewFile(DocumentState document)
        {
            _logService.LogInfo("Creating new file");
            document.Text = string.Empty;
            document.LastSavedText = document.Text;
            document.FilePath = null;
            document.Encoding = new UTF8Encoding(false);

        }
    }
}

using STEditor.Models;

namespace STEditor.Services.Interfaces
{
    public interface IDocumentService
    {
        void OpenFile(DocumentState document, string path);
        void OpenFileFromPath(DocumentState document, string path);
        void SaveFile(DocumentState document);
        void SaveFileAs(DocumentState document, string path);
        void NewFile(DocumentState document);
    }
}

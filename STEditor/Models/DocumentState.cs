using STEditor.Infrastructure;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace STEditor.Models
{
    public class DocumentState
    {

        public string Text { get; set; } = string.Empty;
        public string LastSavedText { get; set; } = string.Empty;
        public string? FilePath { get; set; }
        public Encoding Encoding { get; set; } = new UTF8Encoding(false);
        public EditorHighlightKind HighlightKind { get; set; } = EditorHighlightKind.None;
        public bool HasUnsavedChanges => Text != LastSavedText;
        public string FileName => FilePath != null ? Path.GetFileName(FilePath) : "Untitled";
        public DocumentState() { }
    }
}

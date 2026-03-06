using System.Text;

namespace Simple_Text_Editor.Services
{
    public interface IFileService
    {
        (string Text, Encoding Encoding) ReadFile(string filePath);
        void WriteAllText(string filePath, string text, Encoding encoding);
        void RegisterForOpenWith();
    }
}
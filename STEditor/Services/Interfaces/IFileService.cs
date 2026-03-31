using System.Text;
using System.Windows;

namespace STEditor.Services
{
    public interface IFileService
    {
        string ReadFile(string filePath);
        void WriteToFile(string filePath, string text, Encoding encoding);

    }
}
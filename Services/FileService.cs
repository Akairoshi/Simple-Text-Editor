using Microsoft.Win32;
using Simple_Text_Editor.Infrastructure;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Simple_Text_Editor.Services
{
    public sealed class FileService : IFileService
    {
        public (string Text, Encoding Encoding) ReadFile(string filePath)
        {
            Encoding encoding = EncodingDetector.DetectEncoding(filePath);
            string text = File.ReadAllText(filePath, encoding);

            return (text, encoding);
        }

        public void WriteAllText(string filePath, string text, Encoding encoding)
        {
            File.WriteAllText(filePath, text, encoding);
        }
        public void RegisterForOpenWith()
        {
            string exePath = Process.GetCurrentProcess().MainModule?.FileName
                             ?? throw new InvalidOperationException("Error while get exe path.");

            string exeName = Path.GetFileName(exePath);

            using (var commandKey = Registry.CurrentUser.CreateSubKey(
                $@"Software\Classes\Applications\{exeName}\shell\open\command"))
            {
                commandKey?.SetValue("", $"\"{exePath}\" \"%1\"");
            }

            using (var supportedTypesKey = Registry.CurrentUser.CreateSubKey(
                $@"Software\Classes\Applications\{exeName}\SupportedTypes"))
            {
                supportedTypesKey?.SetValue(".txt", "");
                supportedTypesKey?.SetValue(".md", "");
            }

            NotifyShell();
        }

        private static void NotifyShell()
        {
            SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
        }

        [DllImport("shell32.dll")]
        private static extern void SHChangeNotify(
            uint wEventId,
            uint uFlags,
            IntPtr dwItem1,
            IntPtr dwItem2);
    }
}
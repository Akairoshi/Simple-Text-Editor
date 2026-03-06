using Microsoft.Win32;
using System.Windows;

namespace Simple_Text_Editor.Services
{
    public sealed class DialogService : IDialogService
    {
        public bool TryPickOpenFile(out string filePath)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Text files (*.txt;*.md)|*.txt;*.md|C# files (*.cs)|*.cs|Json files (*.json)|*.json|All files (*.*)|*.*",
                FileName = "Untitled"
            };

            if (dialog.ShowDialog() == true)
            {
                filePath = dialog.FileName;
                return true;
            }

            filePath = string.Empty;
            return false;
        }

        public bool TryPickSaveFile(out string filePath)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Text files (*.txt)|*.txt|MarkDown (*.md)|*.md|C# files (*.cs)|*.cs|Json files (*.json)|*.json|All files (*.*)|*.*",
                FileName = "Untitled"
            };

            if (dialog.ShowDialog() == true)
            {
                filePath = dialog.FileName;
                return true;
            }

            filePath = string.Empty;
            return false;
        }
        public void Show<TWindow>() where TWindow : Window, new()
        {
            var window = new TWindow();
            window.Show();
        }

        public void ShowDialog<TWindow>() where TWindow : Window, new()
        {
            var window = new TWindow();
            window.ShowDialog();
        }

        public void Show<TWindow, TViewModel>(TViewModel viewModel)
            where TWindow : Window, new()
        {
            var window = new TWindow
            {
                DataContext = viewModel
            };

            window.Show();
        }

        public bool? ShowDialog<TWindow, TViewModel>(TViewModel viewModel)
            where TWindow : Window, new()
        {
            var window = new TWindow
            {
                DataContext = viewModel
            };

            return window.ShowDialog();
        }

        public MessageBoxResult Confirm(string message, string title,
            MessageBoxButton buttons = MessageBoxButton.YesNo,
            MessageBoxImage icon = MessageBoxImage.None)
        {
            return MessageBox.Show(message, title, buttons, icon);
        }
    }
}

using Microsoft.Win32;
using STEditor.Services.Interfaces;
using System.Windows;

namespace STEditor.Services
{
    public sealed class DialogService : IDialogService
    {
        private readonly ILogService _logService;
        public DialogService(ILogService logService) 
        {
            _logService = logService;

            _logService.LogInfo("DialogService initialized");
        }
        public bool TryPickOpenFile(out string filePath)
        {
            _logService.LogInfo("Opening file dialog");
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
            _logService.LogInfo("Opening save file dialog");
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
            _logService.LogInfo($"Showing dialog of type {typeof(TWindow).Name}");
            var window = new TWindow();
            window.ShowDialog();
        }

        public void Show<TWindow, TViewModel>(TViewModel viewModel)
            where TWindow : Window, new()
        {
            _logService.LogInfo($"Showing window of type {typeof(TWindow).Name} with view model of type {typeof(TViewModel).Name}");
            var window = new TWindow
            {
                DataContext = viewModel
            };

            window.Show();
        }

        public bool? ShowDialog<TWindow, TViewModel>(TViewModel viewModel)
            where TWindow : Window, new()
        {
            _logService.LogInfo($"Showing dialog of type {typeof(TWindow).Name} with view model of type {typeof(TViewModel).Name}");
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
            _logService.LogInfo($"Showing confirmation dialog with title '{title}' and message '{message}'");
            return MessageBox.Show(message, title, buttons, icon);
        }
    }
}

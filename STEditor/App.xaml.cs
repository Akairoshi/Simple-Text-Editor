using STEditor.Services;
using STEditor.ViewModels;
using STEditor.Views;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using STEditor.Services.Interfaces;
using System.Diagnostics;

namespace STEditor
{
    public partial class App : Application
    {
        private readonly ILogService _logService = new LogService();
        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            _logService.LogError("Unhandled UI exception", e.Exception);
            _logService.ShowDialog("UI Exception");

            e.Handled = true;
            Environment.Exit(1);
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;

            if (ex is not null)
                _logService.LogError("Unhandled exception in AppDomain", ex);
            else
                _logService.LogError("Unhandled exception in AppDomain", new Exception("Unknown exception"));

            _logService.ShowDialog("AppDomain Exception");
            Environment.Exit(1);
        }

        private void OnTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            _logService.LogError("Unobserved task exception", e.Exception);

            _logService.ShowDialog("Task Exeption");

            e.SetObserved();
            Environment.Exit(1);
        }
        public App()
        {
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnTaskException;
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Debug.WriteLine(_logService == null ? "Log service ERROR" : "Log Service OK");
            _logService.LogInfo("Application started");
            IFileService _fileService = new FileService(_logService);
            IDialogService _dialogService = new DialogService(_logService);
            IMarkdownPreviewService _markdownPreviewService = new MarkdownPreviewService(_logService);
            IDocumentService _documentService = new DocumentService(_fileService, _logService);
            ISearchService _searchService = new SearchService(_logService);
            
            var vm = new MainViewModel(_dialogService,
                _markdownPreviewService, _documentService, 
                _logService, _searchService);

            var mainWindow = new MainWindow(vm);
            vm.SetEditorView(mainWindow);

            mainWindow.Loaded += (_, __) =>
            {

                if (e.Args.Length > 0)
                {
                    string filePath = e.Args[0];

                    if (File.Exists(filePath))
                    {
                        vm.OpenFileFromPath(filePath);
                        mainWindow.Editor.Focus();
                    }
                }
            };

            mainWindow.Show();
        }
    }
}
using Simple_Text_Editor.Services;
using Simple_Text_Editor.ViewModels;
using Simple_Text_Editor.Views;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Threading;

namespace Simple_Text_Editor
{
    public partial class App : Application
    {
        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(
                e.Exception.ToString(),
                "WPF UI Exception",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            e.Handled = true;
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;

            MessageBox.Show(
                ex?.ToString() ?? "Unknown exception",
                "AppDomain Exception",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        private void OnTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            MessageBox.Show(
                e.Exception.ToString(),
                "Task Exception",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            e.SetObserved();
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

            var vm = new MainViewModel(
                dialogService: new DialogService(),
                fileService: new FileService());

            var mainWindow = new MainWindow
            {
                DataContext = vm
            };

            mainWindow.Loaded += (_, __) =>
            {


                if (e.Args.Length > 0)
                {
                    string filePath = e.Args[0];

                    if (File.Exists(filePath))
                    {
                        vm.OpenFileFromDrop(filePath);
                        mainWindow.Editor.Focus();
                    }
                }
            };

            mainWindow.Show();
        }
    }
}
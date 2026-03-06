using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using Simple_Text_Editor.Services;
using Simple_Text_Editor.ViewModels;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Xml;
using static Simple_Text_Editor.ViewModels.MainViewModel;

namespace Simple_Text_Editor.Views
{

    public partial class MainWindow : Window
    {
        private MainViewModel Vm => (MainViewModel)DataContext;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            
        }
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _markdownHighlighting = LoadHighlightingFromResource(
                "pack://application:,,,/Assets/Styles/Xshd/MarkDown-Mode.xshd");

            Vm.PropertyChanged += Vm_PropertyChanged;
            ApplyHighlighting(Vm.HighlightKind);
        }

        // Работа с окном
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                if (!vm.CheckChanges())
                {
                    e.Cancel = true;
                }
            }
        }

        private void Window_MouseWheel(object? sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (e.Delta > 0)
                    Vm.ChangeFontSizeCommand.Execute("+");
                else
                    Vm.ChangeFontSizeCommand.Execute("-");

                e.Handled = true;
            }
        }
        
        // Обновление положения указателя 
        private void Editor_Loaded(object sender, RoutedEventArgs e)
        {
            Editor.TextArea.Caret.PositionChanged += Caret_PositionChanged;
        }
        private void Editor_Unloaded(object sender, RoutedEventArgs e)
        {
            Editor.TextArea.Caret.PositionChanged -= Caret_PositionChanged;
        }

        private void Caret_PositionChanged(object? sender, EventArgs e)
        {
            Vm.UpdateCaret(Editor);
        }

        // Подсветка синтаксиса
        private IHighlightingDefinition? _markdownHighlighting;

        private void Vm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.HighlightKind))
            {
                ApplyHighlighting(Vm.HighlightKind);
            }
        }
        private IHighlightingDefinition LoadHighlightingFromResource(string uriString)
        {
            var uri = new Uri(uriString, UriKind.RelativeOrAbsolute);
            var resourceInfo = Application.GetResourceStream(uri);

            if (resourceInfo == null)
                throw new InvalidOperationException($"Resource not found: {uriString}");

            using var reader = new XmlTextReader(resourceInfo.Stream);
            return HighlightingLoader.Load(reader, HighlightingManager.Instance);
        }
        private void ApplyHighlighting(EditorHighlightKind kind)
        {
            Editor.SyntaxHighlighting = kind switch
            {
                EditorHighlightKind.Markdown => _markdownHighlighting,
                EditorHighlightKind.Json => HighlightingManager.Instance.GetDefinition("JavaScript"),
                EditorHighlightKind.CSharp => HighlightingManager.Instance.GetDefinition("C#"),
                _ => null
            };
        }


        // Drag & Drop файлов в окно
        private void Window_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var paths = (string[])e.Data.GetData(DataFormats.FileDrop)!;

                // принимаем только 1 файл и только существующий
                if (paths.Length > 0 && File.Exists(paths[0]))
                {
                    e.Effects = DragDropEffects.Copy;
                    e.Handled = true;
                    return;
                }
            }

            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                return;

            var paths = (string[])e.Data.GetData(DataFormats.FileDrop)!;
            if (paths.Length == 0 || !File.Exists(paths[0]))
                return;

            var path = paths[0];

            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (WindowState == WindowState.Minimized)
                    WindowState = WindowState.Normal;

                Show();
                Activate();
                Topmost = true;
                Topmost = false;
                Focus();

                if (DataContext is MainViewModel vm)
                    vm.OpenFileFromDrop(path);

                Editor.Focus();
            }), DispatcherPriority.ApplicationIdle);
        }
    }
}

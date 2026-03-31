using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using STEditor.Infrastructure;
using STEditor.Services.Interfaces;
using STEditor.ViewModels;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Xml;

namespace STEditor.Views
{

    public partial class MainWindow : Window, IEditorView
    {
        private readonly MainViewModel _vm;

        public int CarretOffset => Editor.CaretOffset;
        public string Text => _vm.EditorText;
        public void Undo()
        {
            if (Editor.CanUndo)
                Editor.Undo();
        }
        public void Redo()
        {
            if (Editor.CanRedo)
                Editor.Redo();
        }
        public void SelectAll()
        {
            Editor.SelectAll();
            Editor.ScrollToHome();
        }
        public void Copy() => Editor.Copy();
        public void Cut() => Editor.Cut();
        public void Paste() => Editor.Paste();
        private MainViewModel Vm => _vm;
        public void SelectText(int startIndex, int length)
        {
            Editor.Select(startIndex, length);
            Editor.ScrollToLine(Editor.Document.GetLineByOffset(startIndex).LineNumber);
        }

        public MainWindow(MainViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            DataContext = _vm;
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
        private void Window_Closing(object? sender, CancelEventArgs e)
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
        private void Editor_Loaded(object? sender, RoutedEventArgs e)
        {
            Editor.TextArea.Caret.PositionChanged += Caret_PositionChanged;
        }
        private void Editor_Unloaded(object? sender, RoutedEventArgs e)
        {
            Editor.TextArea.Caret.PositionChanged -= Caret_PositionChanged;
        }

        private void Caret_PositionChanged(object? sender, EventArgs e)
        {
            int line = Editor.TextArea.Caret.Line;
            int col = Editor.TextArea.Caret.Column;
            Vm.UpdateCaret(line, col);
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
        private void Window_DragOver(object? sender, DragEventArgs e)
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

        private void Window_Drop(object? sender, DragEventArgs e)
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
                    vm.OpenFileFromPath(path);

                Editor.Focus();
            }), DispatcherPriority.ApplicationIdle);
        }
    }
}

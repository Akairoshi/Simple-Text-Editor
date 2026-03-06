using ICSharpCode.AvalonEdit;
using Markdig;
using Simple_Text_Editor.Services;
using Simple_Text_Editor.Views;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Simple_Text_Editor.ViewModels
{
    public sealed class MainViewModel : ViewModelBase
    {
        // Интерфейсы
        private readonly IDialogService _dialogService;
        private readonly IFileService _fileService;

        // Настройки окна редактора
        public enum EditorHighlightKind
        {
            None,
            Markdown,
            Json,
            CSharp
        }
        private EditorHighlightKind _highlightKind;


        private bool _hasUnsavedChanges => EditorText != _lastSavedText;
        private string _lastSavedText = string.Empty;
        private string _editorText = string.Empty;

        private const double DefaultFontSize = 16;
        private double _editorFontSize = DefaultFontSize;
        private string _caretStatus = $"Ln 1, Col 1";
        private string _fontSizeStatus;
        private bool _dockIsVisible = true;
        private bool _isPreviewVisible;

        private string? _currentFilePath;
        private Encoding _currentEncoding = new UTF8Encoding(false);

        public MainViewModel(IDialogService? dialogService, IFileService fileService)
        {
            _dialogService = dialogService;
            _fileService = fileService;

            // Бинды команд для кнопок
            OpenFileCommand = new RelayCommand(_ =>
            {
                if (!CheckChanges())
                    return;

                OpenFile();
            });
            NewFileCommand = new RelayCommand(_ =>
            {
                if (!CheckChanges())
                    return;
                NewFile();
            });
            ExitCommand = new RelayCommand(_ =>
            {
                if (!CheckChanges())
                    return;

                Application.Current.Shutdown();
            });
            SaveCommand = new RelayCommand(_ => SaveFile());
            SaveAsCommand = new RelayCommand(_ => SaveFileAs());
            OpenAboutCommand = new RelayCommand(_ => OpenAboutWindow());
            ChangeFontSizeCommand = new RelayCommand(ChangeFontSize);
            ToggleDockCommand = new RelayCommand(_ => ToggleDock());
            TogglePreviewCommand = new RelayCommand(_ => TogglePreview());
            RegisterOpenWithCommand = new RelayCommand(_ => RegisterOpenWith());

            // первичная инициализация WebView поля предпросмотра MarkDown
            _githubCss = LoadGithubCssFromResource();
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(15) };
            _timer.Tick += (_, _) =>
            {
                _timer.Stop();
                PreviewHtml = BuildHtml(_pendingText, _githubCss, EditorFontSize);
            };

            PreviewHtml = BuildHtml(EditorText, _githubCss, EditorFontSize);

            UpdateFontSizeStatus();
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(CurrentEncoding));
        }

        // Бинды для UI
        public string CurrentEncoding => GetEncodingDisplayName(_currentEncoding);

        public string EditorText
        {
            get => _editorText;
            set
            {
                if (!SetField(ref _editorText, value)) return;
                OnPropertyChanged(nameof(Title));
                _pendingText = value ?? "";
                _timer.Stop();
                _timer.Start();
            }

        }

        public double EditorFontSize
        {
            get => _editorFontSize;
            set
            {
                if (!SetField(ref _editorFontSize, value))
                    return;

                UpdateFontSizeStatus();

                _timer.Stop();
                _timer.Start();
            }
        }
        public string CaretStatus
        {
            get => _caretStatus;
            private set => SetField(ref _caretStatus, value);
        }
        public string FontSizeStatus
        {
            get => _fontSizeStatus;
            private set => SetField(ref _fontSizeStatus, value);
        }
        public string Title
        {
            get
            {
                var fileName = _currentFilePath is null
                    ? "Untitled"
                    : Path.GetFileName(_currentFilePath);

                var star = _hasUnsavedChanges ? "*" : "";
                return $"Simple Text Editor - {fileName}{star}";
            }
        }

        public ICommand OpenFileCommand { get; }
        public ICommand OpenAboutCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand SaveAsCommand { get; }
        public ICommand NewFileCommand { get; }
        public ICommand ExitCommand { get; }
        public ICommand ChangeFontSizeCommand { get; }
        public ICommand ToggleDockCommand { get; }
        public ICommand TogglePreviewCommand { get; }
        public ICommand RegisterOpenWithCommand { get; }

        public bool CheckChanges()
        {
            if (!_hasUnsavedChanges)
                return true;

            var res = _dialogService.Confirm(
                "Save changes??",
                "File changed",
                MessageBoxButton.YesNoCancel);

            if (res == MessageBoxResult.Yes)
            {
                SaveFile();
                return !_hasUnsavedChanges;
            }

            if (res == MessageBoxResult.No)
                return true;

            return false; // Cancel
        }

        private void ChangeFontSize(object? parameter)
        {
            if (parameter == null)
                return;

            var p = parameter.ToString();
            EditorFontSize = p switch
            {
                "+" => Math.Min(EditorFontSize + 2, 150),
                "-" => Math.Max(2, EditorFontSize - 2),
                "=" => DefaultFontSize,
                _ => EditorFontSize
            };
        }

        private void UpdateFontSizeStatus() => FontSizeStatus = $"{EditorFontSize:0} pt";
        
        // Методы для биндов и UI
        private static string GetEncodingDisplayName(Encoding enc)
        {
            return enc.CodePage switch
            {
                65001 => enc.GetPreamble().Length > 0 ? "UTF-8 (BOM)" : "UTF-8",

                1200 => "UTF-16 LE",
                1201 => "UTF-16 BE",

                1251 => "Windows-1251",
                1252 => "Windows-1252",

                20866 => "KOI8-R",
                28591 => "ISO-8859-1",

                _ => $"{enc.WebName.ToUpperInvariant()} (CP{enc.CodePage})"
            };
        }

        public void UpdateCaret(TextEditor editor)
        {
            int line = editor.TextArea.Caret.Line;
            int col = editor.TextArea.Caret.Column;

            CaretStatus = $"Ln {line}, Col {col}";
        }

        //Работа с файлами
        private void RegisterOpenWith()
        {
            _fileService.RegisterForOpenWith();
        }
        private void OpenFileCore(string path)
        {
            var result = _fileService.ReadFile(path);

            EditorText = result.Text;
            _currentEncoding = result.Encoding;
            _currentFilePath = path;
            _lastSavedText = EditorText;

            var ext = Path.GetExtension(path)?.ToLowerInvariant();

            HighlightKind = ext switch
            {
                ".md" => EditorHighlightKind.Markdown,
                ".json" => EditorHighlightKind.Json,
                ".cs" => EditorHighlightKind.CSharp,
                _ => EditorHighlightKind.None
            };

            IsPreviewVisible = ext == ".md";

            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(CurrentEncoding));
        }
        private void OpenFile()
        {
            if (!_dialogService.TryPickOpenFile(out var path))
                return;

            OpenFileCore(path);
        }

        public void OpenFileFromDrop(string path)
        {
            if (!File.Exists(path))
                return;

            if (!CheckChanges())
                return;

            OpenFileCore(path);
        }

        private void SaveFile()
        {
            if (string.IsNullOrWhiteSpace(_currentFilePath))
            {
                SaveFileAs();
                return;
            }

            _fileService.WriteAllText(_currentFilePath, EditorText, _currentEncoding);
            _lastSavedText = EditorText;
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(CurrentEncoding));
        }

        private void SaveFileAs()
        {
            if (!_dialogService.TryPickSaveFile(out var path))
                return;

            _currentFilePath = path;
            _fileService.WriteAllText(path, EditorText, _currentEncoding);
            _lastSavedText = EditorText;
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(CurrentEncoding));
        }

        private void NewFile()
        {
            if (!CheckChanges())
                return;
            EditorText = string.Empty;
            _lastSavedText = EditorText;
            _currentFilePath = null;
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(CurrentEncoding));
        }

        public EditorHighlightKind HighlightKind
        {
            get => _highlightKind;
            set
            {
                if (_highlightKind != value)
                {
                    _highlightKind = value;
                    OnPropertyChanged();
                }
            }
        }
        private void OpenAboutWindow()
        {
            _dialogService.ShowDialog<AboutWindow>();
        }
        private void ToggleDock()
        {
            DockIsVisible = !DockIsVisible;
        }
        private void TogglePreview()
        {
            IsPreviewVisible = !IsPreviewVisible;
        }

        public Visibility DockVisibility =>
            DockIsVisible ? Visibility.Visible : Visibility.Collapsed;
        public bool DockIsVisible
        {
            get => _dockIsVisible;
            set
            {
                _dockIsVisible = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DockVisibility));
            }
        }
        public bool IsPreviewVisible
        {
            get => _isPreviewVisible;
            set
            {
                if (SetField(ref _isPreviewVisible, value))
                {
                    OnPropertyChanged(nameof(RightPaneVisibility));
                    OnPropertyChanged(nameof(RightPaneWidth));
                    OnPropertyChanged(nameof(SplitterWidth));
                }
            }
        }

        public Visibility RightPaneVisibility =>
            IsPreviewVisible ? Visibility.Visible : Visibility.Collapsed;
        public GridLength RightPaneWidth =>
            IsPreviewVisible ? new GridLength(1, GridUnitType.Star) : new GridLength(0);

        public GridLength SplitterWidth =>
            IsPreviewVisible ? new GridLength(5) : new GridLength(0);

        //Конструктор WebView для предосмотра markdown
        private static readonly MarkdownPipeline Pipeline =
        new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UsePipeTables()
            .UseTaskLists()
            .UseAutoLinks()
            .UseEmojiAndSmiley()
            .Build();

        private readonly DispatcherTimer _timer;
        private string _pendingText = "";
        private readonly string _githubCss;

        private string _previewHtml = "";
        public string PreviewHtml
        {
            get => _previewHtml;
            private set => SetField(ref _previewHtml, value);
        }

        private static string LoadGithubCssFromResource()
        {
            try
            {
                var uri = new Uri("pack://application:,,,/Assets/Styles/WebView/github-markdown.css", UriKind.Absolute);
                var streamInfo = Application.GetResourceStream(uri);

                if (streamInfo == null)
                    return "";

                using var reader = new StreamReader(streamInfo.Stream, Encoding.UTF8);
                return reader.ReadToEnd();
            }
            catch
            {

                return "";
            }
        }

        private static string BuildHtml(string markdown, string githubCss, double codeFs)
        {
            var body = Markdig.Markdown.ToHtml(markdown ?? "", Pipeline);

            return $@"
                <!doctype html>
                <html>
                <head>
                <meta charset='utf-8' />
                <style>
                {githubCss}

                body {{ margin:0; padding:0; background:#fff; }}

                .markdown-body {{
                  box-sizing: border-box;
                  min-width: 200px;
                  max-width: 980px;
                  margin: 0 auto;
                  padding: 24px;
                  font-size: {codeFs}px;
                  line-height: 1.55;
                }}

                .markdown-body code {{
                  font-family: Consolas, 'Cascadia Mono', monospace;
                  font-size: {codeFs}px;
                }}

                .markdown-body pre {{
                  padding: 16px;
                  border-radius: 8px;
                  overflow: auto;
                  font-size: {codeFs}px;
                }}
                </style>
                </head>
                <body>
                <article class='markdown-body'>
                {body}
                </article>
                </body>
                </html>";
        }
    }
}

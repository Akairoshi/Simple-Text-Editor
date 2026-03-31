using ICSharpCode.AvalonEdit;
using STEditor.Infrastructure;
using STEditor.Models;
using STEditor.Services;
using STEditor.Services.Interfaces;
using STEditor.Views;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace STEditor.ViewModels
{
    public sealed class MainViewModel : ViewModelBase
    {
        private readonly ILogService _logService;
        private readonly IDialogService _dialogService;
        private readonly IMarkdownPreviewService _markdownPreviewService;
        private readonly IDocumentService _documentService;
        private readonly ISearchService _searchService;
        
        private IEditorView _editorView;

        private readonly DocumentState _documentState = new DocumentState();

        public EditorHighlightKind HighlightKind => _documentState.HighlightKind;

        private const double DefaultFontSize = 16;
        private double _editorFontSize = DefaultFontSize;
        private string _caretStatus = $"Ln 1, Col 1";
        private string _fontSize;
        private bool _dockIsVisible = true;
        private bool _isPreviewVisible;

        public ICommand OpenFileCommand { get; }
        public ICommand OpenAboutCommand { get; }
        public ICommand OpenSearchCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand SaveAsCommand { get; }
        public ICommand NewFileCommand { get; }
        public ICommand ExitCommand { get; }
        public ICommand ChangeFontSizeCommand { get; }
        public ICommand ToggleDockCommand { get; }
        public ICommand TogglePreviewCommand { get; }
        public ICommand RegisterOpenWithCommand { get; }
        public ICommand UndoCommand { get; }
        public ICommand RedoCommand { get; }
        public ICommand CopyCommand { get; }
        public ICommand PasteCommand { get; }
        public ICommand CutCommand { get; }
        public ICommand SelectAllCommand { get; }


        public MainViewModel(
            IDialogService? dialogService, 
            IMarkdownPreviewService markdownPreviewService, 
            IDocumentService documentService,
            ILogService logService,
            ISearchService searchService)
        {
            _logService = logService;
            _documentService = documentService;
            _markdownPreviewService = markdownPreviewService;
            _dialogService = dialogService;
            _searchService = searchService;

            OpenFileCommand = new RelayCommand(_ => OpenFile());
            NewFileCommand = new RelayCommand(_ => NewFile());
            SaveCommand = new RelayCommand(_ => SaveFile());
            SaveAsCommand = new RelayCommand(_ => SaveFileAs());
            OpenAboutCommand = new RelayCommand(_ => OpenAboutWindow());
            OpenSearchCommand = new RelayCommand(_ => OpenSearchWindow());
            ChangeFontSizeCommand = new RelayCommand(ChangeFontSize);
            ToggleDockCommand = new RelayCommand(_ => ToggleDock());
            TogglePreviewCommand = new RelayCommand(_ => TogglePreview());
            ExitCommand = new RelayCommand(_ =>
            {
                if (!CheckChanges())
                    return;

                Application.Current.Shutdown();
            });
            UndoCommand = new RelayCommand(_ => _editorView.Undo());
            RedoCommand = new RelayCommand(_ => _editorView.Redo());
            CopyCommand = new RelayCommand(_ => _editorView.Copy());
            PasteCommand = new RelayCommand(_ => _editorView.Paste());
            CutCommand = new RelayCommand(_ => _editorView.Cut());
            SelectAllCommand = new RelayCommand(_ => _editorView.SelectAll());



            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(15) };
            _timer.Tick += (_, _) =>
            {
                _timer.Stop();
                PreviewHtml = _markdownPreviewService.BuildHtml(EditorText, EditorFontSize);
            };
            PreviewHtml = _markdownPreviewService.BuildHtml(EditorText, EditorFontSize);

            UpdateFontSizeStatus();
            OnPropertyChanged(nameof(Title));

            _logService.LogInfo("MainViewModel initialized successfully");
        }
        public void SetEditorView(IEditorView editorView)
        {
            _editorView = editorView;
        }
        

        public void NewFile()
        {
            if (!CheckChanges())
                return;
            _documentService.NewFile(_documentState);
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(EditorText));
        }

        public void OpenFile()
        {
            if (!CheckChanges())
                return;
            if (!_dialogService.TryPickOpenFile(out var path))
                return;
            _documentService.OpenFile(_documentState, path);
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(EditorText));
        }

        public void SaveFile()
        {
            if (string.IsNullOrWhiteSpace(_documentState.FilePath))
            {
                SaveFileAs();
                return;
            }
            _documentService.SaveFile(_documentState);
            OnPropertyChanged(nameof(Title));
        }

        public void SaveFileAs()
        {
            if (!_dialogService.TryPickSaveFile(out var path))
                return;
            _documentService.SaveFileAs(_documentState, path);
            OnPropertyChanged(nameof(Title));
        }

        public void OpenFileFromPath(string path)
        {
            if (!CheckChanges())
                return;
            _documentService.OpenFileFromPath(_documentState, path);
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(EditorText));
        }

        public string EditorText
        {
            get => _documentState.Text;
            set
            {
                if (_documentState.Text == value)
                    return;

                _documentState.Text = value;
                OnPropertyChanged(nameof(Title));
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
            get => _fontSize;
            private set => SetField(ref _fontSize, value);
        }
        public string Title
        {
            get
            {
                var star = _documentState.HasUnsavedChanges ? "*" : "";
                return $"STEditor - {_documentState.FileName}{star}";
            }
        }

        public bool CheckChanges()
        {
            _logService.LogInfo("Checking for unsaved changes");
            if (!_documentState.HasUnsavedChanges)
                return true;

            var res = _dialogService.Confirm(
                "Save changes??",
                "File changed",
                MessageBoxButton.YesNoCancel);

            if (res == MessageBoxResult.Yes)
            {
                _documentService.SaveFile(_documentState);
                return true;
            }

            if (res == MessageBoxResult.No)
                return true;

            return false;
        }

        private void ChangeFontSize(object? parameter)
        {
            _logService.LogInfo($"ChangeFontSize called with parameter: {parameter}");
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

        public void UpdateCaret(int line, int col)
        {
            CaretStatus = $"Ln {line}, Col {col}";
        }
        private void OpenAboutWindow()
        {
            _logService.LogInfo("Opening About Window");
            _dialogService.ShowDialog<AboutWindow>();
        }
        private void OpenSearchWindow()
        {
            _logService.LogInfo("Opening About Window");
            var searchVm = new SearchViewModel(_searchService, _logService, _editorView);
            _dialogService.Show<SearchWindow, SearchViewModel>(searchVm);
        }
        private void ToggleDock()
        {
            _logService.LogInfo("Toggling dock visibility");
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

        private readonly DispatcherTimer _timer;

        private string _previewHtml = "";
        public string PreviewHtml
        {
            get => _previewHtml;
            private set => SetField(ref _previewHtml, value);
        }
    }
}

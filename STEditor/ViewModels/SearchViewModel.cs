using STEditor.Services.Interfaces;
using System.Windows.Input;

namespace STEditor.ViewModels
{
    public class SearchViewModel : ViewModelBase
    {
        private readonly ILogService _logService;
        private readonly ISearchService _searchService;
        private readonly IEditorView _editorView;
        public enum FindMode { All, Next, Previous, None = -1 }
        public enum Status { NotFound, Found, Invalid }

        private FindMode _currentFindMode = FindMode.All;
        private Status _searchStatus = Status.NotFound;

        private string _query = string.Empty;
        public ICommand FindCommand { get; }
        public ICommand SetFindModeCommand { get; }

        public string SearchStatusText
        {
            get
            {
                return _searchStatus switch
                {
                    Status.NotFound => "Not found",
                    Status.Found => "Found",
                    Status.Invalid => "Invalid",
                    _ => "Search status",
                };
            }
        }
        public string Query 
        {
            get => _query;
            set
            {
                if (!SetField(ref _query, value))
                    return;
            }
        }

        public SearchViewModel(ISearchService searchService,
            ILogService logService,
            IEditorView editorView)
        {
            _searchService = searchService;
            _logService = logService;
            _editorView = editorView;

            FindCommand = new RelayCommand(_ => FindQuery());
            SetFindModeCommand = new RelayCommand(SetFindMode);
        }
        public void SetFindMode(object? parameter)
        {
            if (parameter is string modeStr && Enum.TryParse(modeStr, out FindMode mode))
                CurrentFindMode = mode;
            else
                _logService.LogWarning($"Invalid find mode: {parameter}", new Exception("Invalid find mode"));
        }
        public FindMode CurrentFindMode
        {
            get => _currentFindMode;
            set => SetField(ref _currentFindMode, value);
        }
        public void FindQuery()
        {
            switch (_currentFindMode)
            {
                case FindMode.All:
                    int index = _searchService.Find(_editorView.Text, _query);
                    if (index == -1)
                    {
                        _searchStatus = Status.NotFound;
                        OnPropertyChanged(nameof(SearchStatusText));
                        return;
                    }
                    _searchStatus = Status.Found;
                    OnPropertyChanged(nameof(SearchStatusText));
                    _editorView.SelectText(index, _query.Length);
                    break;
                
                case FindMode.Next:
                    int nextIndex = _searchService.FindNext(_editorView.Text, _query, _editorView.CarretOffset);
                    if (nextIndex == -1)
                    {
                        _searchStatus = Status.NotFound;
                        OnPropertyChanged(nameof(SearchStatusText));
                        return;
                    }
                    _searchStatus = Status.Found;
                    OnPropertyChanged(nameof(SearchStatusText));
                    _editorView.SelectText(nextIndex, _query.Length);
                    break;
                
                case FindMode.Previous:
                    int previousIndex = _searchService.FindPrevious(_editorView.Text, _query, _editorView.CarretOffset-2);
                    if (previousIndex == -1)
                    {
                        _searchStatus = Status.NotFound;
                        OnPropertyChanged(nameof(SearchStatusText));
                        return;
                    }
                    _searchStatus = Status.Found;
                    OnPropertyChanged(nameof(SearchStatusText));
                    _editorView.SelectText(previousIndex, _query.Length);
                    break;

                case FindMode.None:
                default:
                    _searchStatus = Status.Invalid;
                    _logService.LogWarning($"Invalid find mode: {_currentFindMode}", new Exception("Invalid find mode"));
                    break;
            }

        }
    }
}

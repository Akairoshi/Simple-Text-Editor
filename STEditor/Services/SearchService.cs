using STEditor.Services.Interfaces;
namespace STEditor.Services
{
    public class SearchService : ISearchService
    {
        private readonly ILogService _logService;
        public SearchService(ILogService logService) 
        { 
            _logService = logService;

            _logService.LogInfo("SearchService initialized");
        }
        public int Find(string text, string query)
        {
            _logService.LogInfo($"Finding '{query}' in text of length {text?.Length ?? 0}");
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(query))
                return -1;
            return text.IndexOf(query, 0, text.Length, StringComparison.OrdinalIgnoreCase);
        }
        public int FindNext(string text, string query, int startIndex)
        {
            _logService.LogInfo($"Finding next '{query}' in text of length {text?.Length ?? 0} starting at index {startIndex}");
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(query) || startIndex < 0 || startIndex >= text.Length)
                return -1;
            return text.IndexOf(query, startIndex, StringComparison.OrdinalIgnoreCase);
        }
        public int FindPrevious(string text, string query, int endIndex)
        {
            _logService.LogInfo($"Finding previous '{query}' in text of length {text?.Length ?? 0} ending at index {endIndex}");
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(query) || endIndex < 0 || endIndex >= text.Length)
                return -1;
            return text.LastIndexOf(query, endIndex, StringComparison.OrdinalIgnoreCase);
        }

    }
}

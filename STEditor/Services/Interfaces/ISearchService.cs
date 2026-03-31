namespace STEditor.Services.Interfaces
{
    public interface ISearchService
    {
        int Find(string text, string query);
        int FindNext(string text, string query, int startIndex);
        int FindPrevious(string text, string query, int endIndex);
    }
}

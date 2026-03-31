namespace STEditor.Services.Interfaces
{
    public interface IMarkdownPreviewService
    {
        string BuildHtml(string markdown, double codeFs);
    }
}

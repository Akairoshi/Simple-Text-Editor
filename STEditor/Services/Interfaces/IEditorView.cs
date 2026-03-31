namespace STEditor.Services.Interfaces
{
    public interface IEditorView
    {
        int CarretOffset { get; }
        string Text { get; }
        void SelectText(int startIndex, int length);
        void Cut();
        void Copy();
        void Paste();
        void SelectAll();
        void Undo();
        void Redo();
    }
}

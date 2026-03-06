using System.Windows;
using ICSharpCode.AvalonEdit;

namespace Simple_Text_Editor.Infrastructure
{
    public static class AvalonEditBinding
    {
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.RegisterAttached(
                "Text",
                typeof(string),
                typeof(AvalonEditBinding),
                new FrameworkPropertyMetadata(
                    default(string),
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnTextChanged));

        public static string GetText(DependencyObject obj)
        {
            return (string)obj.GetValue(TextProperty);
        }

        public static void SetText(DependencyObject obj, string value)
        {
            obj.SetValue(TextProperty, value);
        }

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TextEditor editor)
                return;

            editor.TextChanged -= Editor_TextChanged;

            if (editor.Text != (string)e.NewValue)
                editor.Text = (string)e.NewValue;

            editor.TextChanged += Editor_TextChanged;
        }

        private static void Editor_TextChanged(object sender, EventArgs e)
        {
            if (sender is TextEditor editor)
                SetText(editor, editor.Text);
        }
    }
}
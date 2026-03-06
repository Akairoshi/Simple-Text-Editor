using Simple_Text_Editor.ViewModels;
using System.Diagnostics;
using System.Windows;

namespace Simple_Text_Editor.Views
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();

            DataContext = new AboutViewModel();

        }
        private void OpenLink_Click(object sender, RoutedEventArgs e)
        {
            var url = "https://github.com/Akairoshi";

            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
    }
}

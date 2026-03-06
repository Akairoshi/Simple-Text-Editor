using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System.IO;
using System.Windows;

namespace Simple_Text_Editor
{
    public static class WebView2HtmlBehavior
    {
        public static readonly DependencyProperty HtmlProperty =
            DependencyProperty.RegisterAttached(
                "Html",
                typeof(string),
                typeof(WebView2HtmlBehavior),
                new PropertyMetadata(null, OnHtmlChanged));

        private static readonly DependencyProperty IsInitializedProperty =
            DependencyProperty.RegisterAttached(
                "IsInitialized",
                typeof(bool),
                typeof(WebView2HtmlBehavior),
                new PropertyMetadata(false));

        private static readonly DependencyProperty IsInitializingProperty =
            DependencyProperty.RegisterAttached(
                "IsInitializing",
                typeof(bool),
                typeof(WebView2HtmlBehavior),
                new PropertyMetadata(false));

        private static CoreWebView2Environment? _env;

        public static void SetHtml(DependencyObject element, string value) =>
            element.SetValue(HtmlProperty, value);

        public static string GetHtml(DependencyObject element) =>
            (string)element.GetValue(HtmlProperty);

        private static async void OnHtmlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not WebView2 webView)
                return;

            try
            {
                if (!(bool)webView.GetValue(IsInitializedProperty))
                {
                    if ((bool)webView.GetValue(IsInitializingProperty))
                        return;

                    webView.SetValue(IsInitializingProperty, true);

                    await EnsureWebView2Async(webView);

                    webView.CoreWebView2.NavigationStarting -= CoreWebView2_NavigationStarting;
                    webView.CoreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;

                    webView.SetValue(IsInitializedProperty, true);
                    webView.SetValue(IsInitializingProperty, false);
                }

                var html = GetHtml(webView) ?? "<html><body></body></html>";
                webView.NavigateToString(html);
            }
            catch (Exception ex)
            {
                webView.SetValue(IsInitializingProperty, false);

                MessageBox.Show(
                    ex.ToString(),
                    "WebView2 error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private static async Task EnsureWebView2Async(WebView2 webView)
        {
            if (webView.CoreWebView2 != null)
                return;

            if (_env == null)
            {
                string userDataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SimpleTextEditor",
                    "WebView2");

                Directory.CreateDirectory(userDataFolder);

                var options = new CoreWebView2EnvironmentOptions
                {
                    AdditionalBrowserArguments =
                        "--disable-features=msEdgePDFViewer " +
                        "--disable-extensions " +
                        "--disable-component-update"
                };

                _env = await CoreWebView2Environment.CreateAsync(
                    null,
                    userDataFolder,
                    options);
            }

            await webView.EnsureCoreWebView2Async(_env);
        }

        private static void CoreWebView2_NavigationStarting(
            object? sender,
            CoreWebView2NavigationStartingEventArgs e)
        {
            if (e.Uri.StartsWith("http", StringComparison.OrdinalIgnoreCase) ||
                e.Uri.StartsWith("https", StringComparison.OrdinalIgnoreCase))
            {
                e.Cancel = true;
            }
        }
    }
}
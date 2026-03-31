using Markdig;
using STEditor.Services.Interfaces;
using System.IO;
using System.Text;
using System.Windows;

namespace STEditor.Services
{
    public class MarkdownPreviewService : IMarkdownPreviewService
    {
        private readonly ILogService _logService;
        public MarkdownPreviewService(ILogService logService)
        {
            _logService = logService;

            _logService.LogInfo("FileService initialized");
        }

        private string _githubCss = LoadGithubCssFromResource();
        private readonly MarkdownPipeline Pipeline =
        new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UsePipeTables()
            .UseTaskLists()
            .UseAutoLinks()
            .UseEmojiAndSmiley()
            .Build();

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

        public string BuildHtml(string markdown, double codeFs)
        {
            var body = Markdig.Markdown.ToHtml(markdown ?? "", Pipeline);

            return $@"
                <!doctype html>
                <html>
                <head>
                <meta charset='utf-8' />
                <style>
                {_githubCss}

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

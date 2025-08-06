using AutoHPMA.Models;
using System.ComponentModel;
using System.Net;
using System.Text;
using System.Windows;
using Wpf.Ui.Controls;
using Microsoft.Web.WebView2.Core;

namespace AutoHPMA.Views.Windows
{
    public partial class UpdateWindow : FluentWindow, INotifyPropertyChanged
    {
        public enum UpdateResult
        {
            Cancel,
            Update,
            Download
        }

        private UpdateResult _result = UpdateResult.Cancel;
        private readonly GitHubRelease _release;

        public string VersionTitle => $"发现新版本 {_release.TagName}";
        public string PublishDate => $"发布时间：{_release.PublishedAt:yyyy年MM月dd日}";

        public UpdateWindow(GitHubRelease release)
        {
            _release = release;
            InitializeComponent();
            DataContext = this;
            _ = InitializeWebViewAsync();
        }

        public async Task<UpdateResult> ShowDialogAsync()
        {
            ShowDialog();
            return _result;
        }

        private async Task InitializeWebViewAsync()
        {
            try
            {
                // 确保WebView2运行时环境已安装
                await ReleaseNotesWebBrowser.EnsureCoreWebView2Async();
                
                // 配置WebView2设置
                ReleaseNotesWebBrowser.CoreWebView2.Settings.IsGeneralAutofillEnabled = false;
                ReleaseNotesWebBrowser.CoreWebView2.Settings.IsPasswordAutosaveEnabled = false;
                ReleaseNotesWebBrowser.CoreWebView2.Settings.AreDevToolsEnabled = false;
                ReleaseNotesWebBrowser.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                ReleaseNotesWebBrowser.CoreWebView2.Settings.AreHostObjectsAllowed = false;
                ReleaseNotesWebBrowser.CoreWebView2.Settings.IsWebMessageEnabled = false;
                
                // 初始化完成后加载内容
                await LoadReleaseNotesAsync();
            }
            catch (WebView2RuntimeNotFoundException)
            {
                // WebView2运行时未安装
                System.Diagnostics.Debug.WriteLine("WebView2运行时未安装");
                await LoadWebView2NotInstalledContentAsync();
            }
            catch (Exception ex)
            {
                // 其他WebView2初始化失败
                System.Diagnostics.Debug.WriteLine($"WebView2初始化失败: {ex.Message}");
                await LoadFallbackContentAsync();
            }
        }

        private async Task LoadReleaseNotesAsync()
        {
            try
            {
                var html = GenerateReleaseNotesHtml(_release.Body);
                ReleaseNotesWebBrowser.NavigateToString(html);
            }
            catch (Exception)
            {
                await LoadFallbackContentAsync();
            }
        }

        private async Task LoadFallbackContentAsync()
        {
            try
            {
                var fallbackHtml = GenerateFallbackHtml();
                ReleaseNotesWebBrowser.NavigateToString(fallbackHtml);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载备用内容失败: {ex.Message}");
            }
        }

        private async Task LoadWebView2NotInstalledContentAsync()
        {
            try
            {
                var webView2NotInstalledHtml = GenerateWebView2NotInstalledHtml();
                ReleaseNotesWebBrowser.NavigateToString(webView2NotInstalledHtml);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载WebView2未安装内容失败: {ex.Message}");
                // 如果连这个都失败了，尝试加载基本的备用内容
                await LoadFallbackContentAsync();
            }
        }

        private string GenerateReleaseNotesHtml(string markdown)
        {
            // 简单的Markdown到HTML转换
            var html = WebUtility.HtmlEncode(markdown);
            
            // 转换一些基本的Markdown语法
            html = html.Replace("\\n", "<br/>");
            html = System.Text.RegularExpressions.Regex.Replace(html, @"### (.+)", "<h3>$1</h3>");
            html = System.Text.RegularExpressions.Regex.Replace(html, @"## (.+)", "<h2>$1</h2>");
            html = System.Text.RegularExpressions.Regex.Replace(html, @"# (.+)", "<h1>$1</h1>");
            html = System.Text.RegularExpressions.Regex.Replace(html, @"\*\*(.+?)\*\*", "<strong>$1</strong>");
            html = System.Text.RegularExpressions.Regex.Replace(html, @"\*(.+?)\*", "<em>$1</em>");
            html = System.Text.RegularExpressions.Regex.Replace(html, @"- (.+)", "<li>$1</li>");

            // 检测当前主题（简单检测，可以根据需要改进）
            var isDarkTheme = IsSystemDarkTheme();

            return $@"
<!DOCTYPE html>
<html lang='zh-CN'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>更新日志</title>
    <style>
        body {{
            font-family: 'Segoe UI', 'Microsoft YaHei', Tahoma, Geneva, Verdana, sans-serif;
            line-height: 1.6;
            color: {(isDarkTheme ? "#ffffff" : "#1f1f1f")};
            margin: 12px;
            padding: 8px;
            background-color: {(isDarkTheme ? "#2d2d30" : "#ffffff")};
            font-size: 14px;
        }}
        h1, h2, h3 {{
            color: {(isDarkTheme ? "#ffffff" : "#1f1f1f")};
            margin-top: 16px;
            margin-bottom: 8px;
            font-weight: 600;
        }}
        h1 {{ font-size: 20px; }}
        h2 {{ font-size: 18px; }}
        h3 {{ font-size: 16px; }}
        p {{ 
            margin-bottom: 10px; 
            margin-top: 0;
        }}
        li {{ 
            margin-bottom: 4px;
            list-style-type: disc;
        }}
        ul {{
            padding-left: 20px;
            margin-top: 8px;
            margin-bottom: 12px;
        }}
        strong {{ 
            color: {(isDarkTheme ? "#4fc3f7" : "#0078d4")};
            font-weight: 600;
        }}
        em {{ 
            color: {(isDarkTheme ? "#81c784" : "#107c10")};
            font-style: italic;
        }}
        code {{
            background-color: {(isDarkTheme ? "#3c3c3c" : "#f3f2f1")};
            color: {(isDarkTheme ? "#d4d4d4" : "#323130")};
            padding: 2px 4px;
            border-radius: 3px;
            font-family: 'Consolas', 'Courier New', monospace;
            font-size: 13px;
        }}
        blockquote {{
            border-left: 3px solid {(isDarkTheme ? "#4fc3f7" : "#0078d4")};
            margin: 12px 0;
            padding-left: 12px;
            color: {(isDarkTheme ? "#cccccc" : "#605e5c")};
        }}
        hr {{
            border: none;
            border-top: 1px solid {(isDarkTheme ? "#484848" : "#edebe9")};
            margin: 16px 0;
        }}
        ::-webkit-scrollbar {{
            width: 8px;
        }}
        ::-webkit-scrollbar-track {{
            background: {(isDarkTheme ? "#2d2d30" : "#f1f1f1")};
        }}
        ::-webkit-scrollbar-thumb {{
            background: {(isDarkTheme ? "#484848" : "#c1c1c1")};
            border-radius: 4px;
        }}
        ::-webkit-scrollbar-thumb:hover {{
            background: {(isDarkTheme ? "#5a5a5a" : "#a8a8a8")};
        }}
    </style>
</head>
<body>
    {html}
</body>
</html>";
        }

        private string GenerateFallbackHtml()
        {
            var isDarkTheme = IsSystemDarkTheme();
            
            return $@"
<!DOCTYPE html>
<html lang='zh-CN'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>更新日志</title>
    <style>
        body {{
            font-family: 'Segoe UI', 'Microsoft YaHei', Tahoma, Geneva, Verdana, sans-serif;
            line-height: 1.6;
            color: {(isDarkTheme ? "#ffffff" : "#1f1f1f")};
            margin: 16px;
            background-color: {(isDarkTheme ? "#2d2d30" : "#ffffff")};
            text-align: center;
            padding-top: 50px;
        }}
        .message {{
            font-size: 16px;
            color: {(isDarkTheme ? "#cccccc" : "#605e5c")};
        }}
    </style>
</head>
<body>
    <div class='message'>
        无法加载更新日志，请前往GitHub查看详细信息
    </div>
</body>
</html>";
        }

        private string GenerateWebView2NotInstalledHtml()
        {
            var isDarkTheme = IsSystemDarkTheme();
            
            return $@"
<!DOCTYPE html>
<html lang='zh-CN'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>WebView2未安装</title>
    <style>
        body {{
            font-family: 'Segoe UI', 'Microsoft YaHei', Tahoma, Geneva, Verdana, sans-serif;
            line-height: 1.6;
            color: {(isDarkTheme ? "#ffffff" : "#1f1f1f")};
            margin: 16px;
            background-color: {(isDarkTheme ? "#2d2d30" : "#ffffff")};
            text-align: center;
            padding-top: 30px;
        }}
        .title {{
            font-size: 18px;
            font-weight: 600;
            color: {(isDarkTheme ? "#ff6b6b" : "#d13438")};
            margin-bottom: 16px;
        }}
        .message {{
            font-size: 14px;
            color: {(isDarkTheme ? "#cccccc" : "#605e5c")};
            margin-bottom: 8px;
        }}
        .note {{
            font-size: 12px;
            color: {(isDarkTheme ? "#999999" : "#8a8886")};
            margin-top: 16px;
        }}
    </style>
</head>
<body>
    <div class='title'>WebView2运行时未安装</div>
    <div class='message'>无法显示更新内容，因为系统缺少WebView2运行时。</div>
    <div class='message'>请安装Microsoft Edge WebView2运行时后重试。</div>
    <div class='note'>您仍然可以使用下方的按钮进行更新或手动下载。</div>
</body>
</html>";
        }

        private bool IsSystemDarkTheme()
        {
            try
            {
                // 检查WPF-UI主题设置
                var theme = Wpf.Ui.Appearance.ApplicationThemeManager.GetAppTheme();
                if (theme == Wpf.Ui.Appearance.ApplicationTheme.Dark)
                    return true;
                if (theme == Wpf.Ui.Appearance.ApplicationTheme.Light)
                    return false;

                // 如果是系统主题，检查系统设置
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                var value = key?.GetValue("AppsUseLightTheme");
                return value is int intValue && intValue == 0;
            }
            catch
            {
                // 默认返回浅色主题
                return false;
            }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            _result = UpdateResult.Update;
            Close();
        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            _result = UpdateResult.Download;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _result = UpdateResult.Cancel;
            Close();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
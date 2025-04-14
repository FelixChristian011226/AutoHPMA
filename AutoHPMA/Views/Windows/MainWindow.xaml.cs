// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using AutoHPMA.ViewModels.Windows;
using Newtonsoft.Json;
using System.Net.Http;
using System.Reflection;
using Wpf.Ui;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace AutoHPMA.Views.Windows
{

    public class GitHubRelease
    {
        [JsonProperty("tag_name")]
        public string TagName { get; set; }

        // 你也可以添加其它你感兴趣的字段，例如发布说明、下载链接等
    }

    public partial class MainWindow : INavigationWindow
    {

        private const string GitHubApiUrl = "https://api.github.com/repos/FelixChristian011226/AutoHPMA/releases/latest";

        public MainWindowViewModel ViewModel { get; }

        public MainWindow(
            MainWindowViewModel viewModel,
            INavigationViewPageProvider navigationViewPageProvider,
            INavigationService navigationService
        )
        {
            ViewModel = viewModel;
            DataContext = this;

            SystemThemeWatcher.Watch(this);

            InitializeComponent();
            SetPageService(navigationViewPageProvider);

            navigationService.SetNavigationControl(RootNavigation);

            CheckForUpdatesAsync();
        }

        #region INavigationWindow methods

        public INavigationView GetNavigation() => RootNavigation;

        public bool Navigate(Type pageType) => RootNavigation.Navigate(pageType);

        public void SetPageService(INavigationViewPageProvider navigationViewPageProvider) => RootNavigation.SetPageProviderService(navigationViewPageProvider);

        public void ShowWindow() => Show();

        public void CloseWindow() => Close();

        #endregion INavigationWindow methods

        /// <summary>
        /// Raises the closed event.
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // Make sure that closing this window will begin the process of closing the application.
            Application.Current.Shutdown();
        }

        INavigationView INavigationWindow.GetNavigation()
        {
            throw new NotImplementedException();
        }

        public void SetServiceProvider(IServiceProvider serviceProvider)
        {
            throw new NotImplementedException();
        }

        private async Task CheckForUpdatesAsync()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    // GitHub要求请求头中必须包含User-Agent信息
                    client.DefaultRequestHeaders.Add("User-Agent", "AutoHPMA");

                    // 发送请求，获取最新release信息
                    var json = await client.GetStringAsync(GitHubApiUrl);
                    var latestRelease = JsonConvert.DeserializeObject<GitHubRelease>(json);

                    // 去掉 "v" 前缀并转换为 Version 对象（前提是版本号格式满足语义化规范）
                    var latestVersionStr = latestRelease.TagName.TrimStart('v');
                    var latestVersion = new Version(latestVersionStr);

                    // 获取当前应用版本 (可以在Assembly信息中设置版本号)
                    var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;

                    // 比较版本
                    if (latestVersion > currentVersion)
                    {
                        // 弹出提示或以其它方式通知用户有更新
                        System.Windows.MessageBox.Show($"检测到新版本: {latestRelease.TagName}！请前往GitHub下载最新版本。",
                            "发现更新", System.Windows.MessageBoxButton.OK, MessageBoxImage.Information);

                        // 可选：自动打开下载页面
                        // System.Diagnostics.Process.Start(new ProcessStartInfo("https://github.com/YourUsername/YourRepo/releases/latest") { UseShellExecute = true });
                    }
                    else
                    {
                        // 如果没有更新，可以选择不做提示，或仅在调试/测试时显示信息
                        // MessageBox.Show("当前已是最新版本。");
                    }

                }
            }
            catch (Exception ex)
            {
                // 捕获异常，记录日志或在调试阶段查看
                System.Windows.MessageBox.Show($"更新检测失败：{ex.Message}", "错误", System.Windows.MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}

// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using AutoHPMA.GameTask;
using AutoHPMA.Helpers.CaptureHelper;
using AutoHPMA.Services;
using AutoHPMA.ViewModels.Pages;
using AutoHPMA.ViewModels.Windows;
using AutoHPMA.Views.Pages;
using AutoHPMA.Views.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using System.IO;
using System.Reflection;
using System.Windows.Threading;
using Wpf.Ui;
using Wpf.Ui.DependencyInjection;
using AutoHPMA.Config;

namespace AutoHPMA
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {


        // The.NET Generic Host provides dependency injection, configuration, logging, and other services.
        // https://docs.microsoft.com/dotnet/core/extensions/generic-host
        // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
        // https://docs.microsoft.com/dotnet/core/extensions/configuration
        // https://docs.microsoft.com/dotnet/core/extensions/logging
        private static readonly IHost _host = Host
            .CreateDefaultBuilder()
            .ConfigureAppConfiguration(c => 
            { 
                c.SetBasePath(Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location));
                c.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                // 注册配置服务
                services.AddSingleton<AppSettings>(sp => AppSettings.Load());

                var logWindow = new LogWindow();
                services.AddSingleton(logWindow);

                var maskWindow = new MaskWindow();
                services.AddSingleton(maskWindow);

                services.AddNavigationViewPageProvider();

                services.AddLogging(c => c.AddSerilog());
                services.AddHostedService<ApplicationHostService>();

                // Theme manipulation
                services.AddSingleton<IThemeService, ThemeService>();

                // TaskBar manipulation
                services.AddSingleton<ITaskBarService, TaskBarService>();

                // Service containing navigation, same as INavigationWindow... but without window
                services.AddSingleton<INavigationService, NavigationService>();
                services.AddSingleton<ISnackbarService, SnackbarService>();

                // Main window with navigation
                services.AddSingleton<INavigationWindow, MainWindow>();
                services.AddSingleton<MainWindowViewModel>();

                services.AddSingleton<DashboardPage>();
                services.AddSingleton<DashboardViewModel>();
                services.AddSingleton<TaskPage>();
                services.AddSingleton<TaskViewModel>();
                services.AddSingleton<ScreenshotPage>();
                services.AddSingleton<ScreenshotViewModel>();
                services.AddSingleton<TestPage>();
                services.AddSingleton<TestViewModel>();
                services.AddSingleton<SettingsPage>();
                services.AddSingleton<SettingsViewModel>();
            }).Build();

        public static ILogger<T> GetLogger<T>()
        {
            return _host.Services.GetService<ILogger<T>>()!;
        }

        public static IServiceProvider Services
        {
            get { return _host.Services; }
        }

        /// <summary>
        /// Occurs when the application is loading.
        /// </summary>
        private async void OnStartup(object sender, StartupEventArgs e)
        {
            await _host.StartAsync();

            // 检查是否显示过使用条款
            var settings = AppSettings.Load();
            if (!settings.HasShownTermsOfUse)
            {
                var uiMessageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "使用条款",
                    Content = 
                    "1. 本软件仅供学习和研究使用\n" +
                    "2. 请勿将本软件用于任何非法用途\n" +
                    "3. 使用本软件产生的任何后果由用户自行承担\n" +
                    "4. 本软件不保证100%的准确性和稳定性\n\n" +
                    "点击确定表示您同意以上条款。",
                    PrimaryButtonText = "确定",
                    CloseButtonText = "退出",
                };

                var result = await uiMessageBox.ShowDialogAsync();
                if(result == Wpf.Ui.Controls.MessageBoxResult.Primary)
                {
                    settings.HasShownTermsOfUse = true;
                    settings.Save();
                }
                else
                {
                    Application.Current.Shutdown();
                }
            }
        }

        /// <summary>
        /// Occurs when the application is closing.
        /// </summary>
        private async void OnExit(object sender, ExitEventArgs e)
        {
            await _host.StopAsync();

            _host.Dispose();
        }

        /// <summary>
        /// Occurs when an exception is thrown by an application but not handled.
        /// </summary>
        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // For more info see https://docs.microsoft.com/en-us/dotnet/api/system.windows.application.dispatcherunhandledexception?view=windowsdesktop-6.0
        }
    }
}

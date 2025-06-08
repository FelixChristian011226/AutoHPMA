// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using AutoHPMA.Messages;
using AutoHPMA.ViewModels.Windows;
using CommunityToolkit.Mvvm.Messaging;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using Wpf.Ui;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using System.Windows.Interop;
using AutoHPMA.Services;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using AutoHPMA.ViewModels.Pages;
using System.Runtime.InteropServices;
using System.Linq;
using System.Collections.Concurrent;

namespace AutoHPMA.Views.Windows
{

    public class GitHubRelease
    {
        [JsonProperty("tag_name")]
        public string TagName { get; set; }

    }

    public partial class MainWindow : INavigationWindow
    {

        private readonly ISnackbarService _snackbarService;
        private readonly ILogger<MainWindow> _logger;
        private HotkeySettingsViewModel _hotkeySettings;
        private IntPtr _hookHandle = IntPtr.Zero;
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;
        private readonly ConcurrentQueue<string> _actionQueue = new();
        private System.Windows.Threading.DispatcherTimer _actionTimer;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private LowLevelKeyboardProc _proc;

        private const string GitHubApiUrl = "https://api.github.com/repos/FelixChristian011226/AutoHPMA/releases/latest";

        public MainWindowViewModel ViewModel { get; }

        public MainWindow(
            MainWindowViewModel viewModel,
            INavigationViewPageProvider navigationViewPageProvider,
            INavigationService navigationService,
            ISnackbarService snackbarService)
        {
            ViewModel = viewModel;
            DataContext = this;
            _logger = App.GetLogger<MainWindow>();
            _hotkeySettings = (HotkeySettingsViewModel)App.Services.GetService(typeof(HotkeySettingsViewModel));

            InitializeComponent();

            SystemThemeWatcher.Watch(this);

            _snackbarService = snackbarService;
            _snackbarService.SetSnackbarPresenter(SnackbarPresenter);

            // 注册消息接收器
            WeakReferenceMessenger.Default.Register<ShowSnackbarMessage>(this, (r, message) =>
            {
                var info = message.Value;
                _snackbarService.Show(
                    info.Title,
                    info.Message,
                    info.Appearance,
                    info.Icon,
                    info.Duration);
            });

            SetPageService(navigationViewPageProvider);
            navigationService.SetNavigationControl(RootNavigation);

            CheckForUpdatesAsync();
            SetupKeyboardHook();
            SetupActionTimer();
        }

        private void SetupActionTimer()
        {
            _actionTimer = new System.Windows.Threading.DispatcherTimer();
            _actionTimer.Interval = TimeSpan.FromMilliseconds(100);
            _actionTimer.Tick += ActionTimer_Tick;
            _actionTimer.Start();
        }

        private void ActionTimer_Tick(object sender, EventArgs e)
        {
            while (_actionQueue.TryDequeue(out string actionName))
            {
                try
                {
                    _logger.LogDebug($"Executing hotkey action: {actionName}");
                    _hotkeySettings?.ExecuteHotkeyAction(actionName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error executing hotkey action: {actionName}");
                }
            }
        }

        private void SetupKeyboardHook()
        {
            _proc = HookCallback;
            _hookHandle = SetHook(_proc);
            if (_hookHandle == IntPtr.Zero)
            {
                _logger.LogError("Failed to set keyboard hook");
            }
            else
            {
                _logger.LogDebug("Keyboard hook set successfully");
            }
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Key key = KeyInterop.KeyFromVirtualKey(vkCode);
                _logger.LogDebug($"Key pressed: {key}");

                if (_hotkeySettings != null)
                {
                    var binding = _hotkeySettings.HotkeyBindings.FirstOrDefault(b => b.Key == key);
                    if (binding != null)
                    {
                        _logger.LogDebug($"Found matching hotkey binding: {binding.Name}");
                        _actionQueue.Enqueue(binding.Name);
                    }
                }
            }
            return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
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
            if (_hookHandle != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookHandle);
                _hookHandle = IntPtr.Zero;
            }
            _actionTimer?.Stop();
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

                    var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;

                    if (latestVersion > currentVersion)
                    {

                        var uiMessageBox = new Wpf.Ui.Controls.MessageBox
                        {
                            Title = "发现更新",
                            Content = "检测到新版本：" + latestRelease.TagName + "! \n请前往GitHub下载最新版本。",
                            IsSecondaryButtonEnabled = false,
                            PrimaryButtonText = "确定",
                            CloseButtonText = "取消",
                        };
                        var result = await uiMessageBox.ShowDialogAsync();


                        if (result == Wpf.Ui.Controls.MessageBoxResult.Primary)
                        {
                            System.Diagnostics.Process.Start(new ProcessStartInfo("https://github.com/FelixChristian011226/AutoHPMA/releases/latest")
                            {
                                UseShellExecute = true
                            });
                        }

                        return;

                    }
                    else
                    {
                        _snackbarService.Show(
                            title: "检查更新",
                            message: "当前已是最新版本。",
                            ControlAppearance.Success,
                            icon: new SymbolIcon(SymbolRegular.ArrowCircleUp24, 36),
                            timeout: TimeSpan.FromSeconds(3)
                            );

                        // MessageBox.Show("当前已是最新版本。");
                    }

                }
            }
            catch (Exception ex)
            {
                // 捕获异常，记录日志或在调试阶段查看
                var uiMessageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "更新检测失败",
                    Content = ex.Message,
                    IsSecondaryButtonEnabled = false,
                    CloseButtonText = "确定",
                };
                var result = await uiMessageBox.ShowDialogAsync();
            }
        }

    }
}

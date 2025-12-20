// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using AutoHPMA.Config;
using AutoHPMA.Helpers;
using AutoHPMA.Helpers.CaptureHelper;
using AutoHPMA.Messages;
using AutoHPMA.Services;
using AutoHPMA.Views.Windows;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using OpenCvSharp.Extensions;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Wpf.Ui.Controls;

namespace AutoHPMA.ViewModels.Pages
{
    public partial class DashboardViewModel : ObservableObject
    {
        #region 字段

        private readonly AppSettings _settings;
        private readonly ILogger<DashboardViewModel> _logger;
        private DispatcherTimer _syncWindowTimer;
        private DispatcherTimer _captureTimer;
        private Bitmap? bmp;

        #endregion

        #region Observable Properties

        [ObservableProperty]
        private bool _realTimeScreenshotEnabled = true;

        [ObservableProperty]
        private bool _logWindowEnabled = true;

        [ObservableProperty]
        private bool _logWindowMarqueeEnabled = true;

        [ObservableProperty]
        private bool _debugLogEnabled = false;

        [ObservableProperty]
        private bool _maskWindowEnabled = false;

        [ObservableProperty]
        private int _captureInterval = 500;

        [ObservableProperty]
        private Visibility _startButtonVisibility = Visibility.Visible;

        [ObservableProperty]
        private Visibility _stopButtonVisibility = Visibility.Collapsed;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(StartTriggerCommand))]
        private bool _startButtonEnabled = true;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(StopTriggerCommand))]
        private bool _stopButtonEnabled = true;

        #endregion

        #region AppContextService 属性

        private LogWindow? _logWindow
        {
            get => AppContextService.Instance.LogWindow;
            set => AppContextService.Instance.LogWindow = value;
        }

        private MaskWindow? _maskWindow
        {
            get => AppContextService.Instance.MaskWindow;
            set => AppContextService.Instance.MaskWindow = value;
        }

        private WindowsGraphicsCapture _capture
        {
            get => AppContextService.Instance.Capture;
            set => AppContextService.Instance.Capture = value;
        }

        private IntPtr _displayHwnd
        {
            get => AppContextService.Instance.DisplayHwnd;
            set => AppContextService.Instance.DisplayHwnd = value;
        }

        private IntPtr _gameHwnd
        {
            get => AppContextService.Instance.GameHwnd;
            set => AppContextService.Instance.GameHwnd = value;
        }

        #endregion

        #region 枚举与事件

        private enum StartupOption { OfficialLauncher, MumuSimulator, None }
        private StartupOption _startupOption = StartupOption.None;

        public static event Action<Bitmap>? ScreenshotUpdated;

        #endregion

        #region 构造函数

        public DashboardViewModel(AppSettings settings, ILogger<DashboardViewModel> logger)
        {
            _settings = settings;
            _logger = logger;
            LoadSettings();
        }

        private void LoadSettings()
        {
            CaptureInterval = _settings.CaptureInterval;
            RealTimeScreenshotEnabled = _settings.RealTimeScreenshotEnabled;
            LogWindowEnabled = _settings.LogWindowEnabled;
            LogWindowMarqueeEnabled = _settings.LogWindowMarqueeEnabled;
            DebugLogEnabled = _settings.DebugLogEnabled;
            MaskWindowEnabled = _settings.MaskWindowEnabled;
        }

        #endregion

        #region Timer 处理

        private void InitializeCaptureTimer()
        {
            _captureTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(_captureInterval)
            };
        }

        public void InitializeSyncWindowTimer()
        {
            _syncWindowTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };
        }

        private void CaptureTimer_Tick(object? sender, EventArgs e)
        {
            if (_gameHwnd == IntPtr.Zero || !_realTimeScreenshotEnabled) return;

            bmp?.Dispose();
            bmp = null;

            using var frame = _capture?.Capture();
            if (frame != null)
            {
                bmp = frame.ToBitmap();
                ScreenshotUpdated?.Invoke(bmp);
            }
        }

        private void SyncWindowTimer_Tick(object? sender, EventArgs e)
        {
            if (_gameHwnd == IntPtr.Zero) return;

            bool shouldHide = NativeMethodsService.IsIconic(_gameHwnd) ||
                             NativeMethodsService.GetForegroundWindow() != _displayHwnd;

            if (shouldHide)
            {
                _logWindow?.Hide();
                _maskWindow?.Hide();
            }
            else
            {
                _logWindow?.Show();
                _maskWindow?.Show();
            }

            _logWindow?.RefreshPosition(_gameHwnd);
            _maskWindow?.RefreshPosition(_displayHwnd);
        }

        #endregion

        #region 命令

        private bool CanStartTrigger() => StartButtonEnabled;

        [RelayCommand(CanExecute = nameof(CanStartTrigger))]
        private void OnStartTrigger()
        {
            GetGameHwnd();

            if (_gameHwnd == IntPtr.Zero)
            {
                ShowErrorMessage("未找到游戏窗口。请先启动游戏！");
                return;
            }

            ShowSuccessSnackbar("截图器已启动，可启动其他任务。");

            StartButtonVisibility = Visibility.Collapsed;
            StopButtonVisibility = Visibility.Visible;

            // 当官方启动器时，将游戏窗口置于前端
            if (_startupOption == StartupOption.OfficialLauncher)
            {
                NativeMethodsService.ShowWindow(_gameHwnd, NativeMethodsService.SW_SHOW);
                NativeMethodsService.SetForegroundWindow(_gameHwnd);
            }

            // 隐藏主窗口
            Application.Current.Dispatcher.Invoke(() => Application.Current.MainWindow.Hide());

            InitializeTimersAndWindows();
        }

        private void InitializeTimersAndWindows()
        {
            InitializeCaptureTimer();
            InitializeSyncWindowTimer();

            _captureTimer.Tick += CaptureTimer_Tick;
            _captureTimer.Start();

            if (_logWindowEnabled)
            {
                _syncWindowTimer.Tick += SyncWindowTimer_Tick;
                _syncWindowTimer.Start();

                _logWindow = new LogWindow();
                _logWindow.Show();
                _logWindow.ShowInTaskbar = false;
                _logWindow.RefreshPosition(_gameHwnd);
                _logWindow.ShowDebugLogs = DebugLogEnabled;
                _logWindow.ShowMarquee = LogWindowMarqueeEnabled;
                
                _logger.LogInformation("检测到[Yellow]{StartupOption}[/Yellow]已启动", _startupOption);
                ShowGameWindowInfo();
            }

            if (_maskWindowEnabled)
            {
                _maskWindow = new MaskWindow();
                _maskWindow.Show();
                _maskWindow.ShowInTaskbar = false;
                _maskWindow.RefreshPosition(_displayHwnd);
            }

            _capture = new WindowsGraphicsCapture();
            _capture.Start(_displayHwnd);
        }

        private bool CanStopTrigger() => StopButtonEnabled;

        [RelayCommand(CanExecute = nameof(CanStopTrigger))]
        private void OnStopTrigger()
        {
            StartButtonVisibility = Visibility.Visible;
            StopButtonVisibility = Visibility.Collapsed;

            // 发送停止所有任务的消息
            WeakReferenceMessenger.Default.Send(new StopAllTasksMessage(true));

            CleanupResources();
        }

        private void CleanupResources()
        {
            _logWindow?.Close();
            _logWindow = null;

            _maskWindow?.Close();
            _maskWindow = null;

            _captureTimer.Tick -= CaptureTimer_Tick;
            _captureTimer.Stop();

            _syncWindowTimer.Tick -= SyncWindowTimer_Tick;
            _syncWindowTimer.Stop();

            _capture?.Stop();
            _capture = null;
            
            GC.Collect();
        }

        [RelayCommand]
        public void OnGoToWikiUrl()
        {
            Process.Start(new ProcessStartInfo("https://autohpma-web.vercel.app/") { UseShellExecute = true });
        }

        #endregion

        #region 辅助方法

        private void ShowErrorMessage(string content)
        {
            var uiMessageBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = "⚠️ 错误",
                Content = content,
            };
            _ = uiMessageBox.ShowDialogAsync();
        }

        private void ShowSuccessSnackbar(string message)
        {
            var snackbarInfo = new SnackbarInfo
            {
                Title = "启动成功",
                Message = message,
                Appearance = ControlAppearance.Success,
                Icon = new SymbolIcon(SymbolRegular.CheckmarkCircle24, 36),
                Duration = TimeSpan.FromSeconds(3),
            };
            WeakReferenceMessenger.Default.Send(new ShowSnackbarMessage(snackbarInfo));
        }

        private void GetGameHwnd()
        {
            _displayHwnd = WindowHelper.FindHandleByProcessName("MuMu安卓设备", "MuMuNxDevice");
            if (_displayHwnd != IntPtr.Zero)
            {
                _startupOption = StartupOption.MumuSimulator;
                _gameHwnd = WindowHelper.FindChildWindowByTitle(_displayHwnd, "MuMuNxDevice");
            }
            else
            {
                _gameHwnd = WindowHelper.FindHandleByProcessName("哈利波特：魔法觉醒", "Harry Potter Magic Awakened");
                if (_gameHwnd != IntPtr.Zero)
                {
                    _startupOption = StartupOption.OfficialLauncher;
                }
            }
        }

        private void ShowGameWindowInfo()
        {
            try
            {
                WindowInteractionHelper.GetWindowPositionAndSize(_gameHwnd, out int left, out int top, out int width, out int height);
                _logger.LogDebug("窗口句柄信息 - DisplayHwnd: [Yellow]0x{DisplayHwnd:X}[/Yellow], GameHwnd: [Yellow]0x{GameHwnd:X}[/Yellow]", 
                    _displayHwnd.ToInt64(), _gameHwnd.ToInt64());
                _logger.LogInformation("检测到游戏窗口分辨率 [Yellow]{Width}*{Height}[/Yellow]", width, height);
                _logger.LogDebug("游戏窗口位置：左上角({Left},{Top})", left, top);

                WindowInteractionHelper.GetWindowPositionAndSize(_displayHwnd, out int leftMumu, out int topMumu, out width, out height);
                _logger.LogDebug("检测到模拟器窗口分辨率{Width}*{Height}", width, height);
                _logger.LogDebug("模拟器窗口位置：左上角({Left},{Top})", leftMumu, topMumu);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取窗口信息失败");
            }
        }

        #endregion

        #region 设置保存

        partial void OnCaptureIntervalChanged(int value)
        {
            SaveSetting(() => _settings.CaptureInterval = value);
            _captureTimer?.SetInterval(TimeSpan.FromMilliseconds(value));
        }

        partial void OnRealTimeScreenshotEnabledChanged(bool value) => SaveSetting(() => _settings.RealTimeScreenshotEnabled = value);
        partial void OnLogWindowEnabledChanged(bool value) => SaveSetting(() => _settings.LogWindowEnabled = value);
        partial void OnDebugLogEnabledChanged(bool value) => SaveSetting(() => _settings.DebugLogEnabled = value);
        partial void OnMaskWindowEnabledChanged(bool value) => SaveSetting(() => _settings.MaskWindowEnabled = value);
        partial void OnLogWindowMarqueeEnabledChanged(bool value) => SaveSetting(() => _settings.LogWindowMarqueeEnabled = value);

        private void SaveSetting(Action updateAction)
        {
            updateAction();
            _settings.Save();
        }

        #endregion
    }

    // DispatcherTimer 扩展方法
    internal static class DispatcherTimerExtensions
    {
        public static void SetInterval(this DispatcherTimer timer, TimeSpan interval)
        {
            if (timer != null)
            {
                timer.Interval = interval;
            }
        }
    }
}

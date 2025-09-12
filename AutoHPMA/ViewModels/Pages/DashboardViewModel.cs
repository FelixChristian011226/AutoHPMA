// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using AutoHPMA.Config;
using AutoHPMA.Helpers;
using AutoHPMA.Helpers.CaptureHelper;
using AutoHPMA.Messages;
using AutoHPMA.Services;
using AutoHPMA.Views;
using AutoHPMA.Views.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.WebSockets;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using Wpf.Ui.Controls;
using Rect = OpenCvSharp.Rect;

namespace AutoHPMA.ViewModels.Pages
{

    public partial class DashboardViewModel : ObservableObject
    {

        private readonly AppSettings _settings;
        private readonly ILogger<DashboardViewModel> _logger;
        private DispatcherTimer _syncWindowTimer;
        private DispatcherTimer _captureTimer;

        private Bitmap? bmp;

        # region Observable Properties
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

        # region argument from AppContextService
        private LogWindow? _logWindow
        {
            get => AppContextService.Instance.LogWindow;
            set
            {
                AppContextService.Instance.LogWindow = value;
                OnPropertyChanged(nameof(_logWindow));
            }
        }

        private MaskWindow? _maskWindow
        {
            get => AppContextService.Instance.MaskWindow;
            set
            {
                AppContextService.Instance.MaskWindow = value;
                OnPropertyChanged(nameof(_maskWindow));
            }
        }

        private WindowsGraphicsCapture _capture
        {
            get => AppContextService.Instance.Capture;
            set
            {
                AppContextService.Instance.Capture = value;
                OnPropertyChanged(nameof(_capture));
            }
        }

        private IntPtr _displayHwnd
        {
            get => AppContextService.Instance.DisplayHwnd;
            set
            {
                AppContextService.Instance.DisplayHwnd = value;
                OnPropertyChanged(nameof(_displayHwnd));
            }
        }

        private IntPtr _gameHwnd
        {
            get => AppContextService.Instance.GameHwnd;
            set
            {
                AppContextService.Instance.GameHwnd = value;
                OnPropertyChanged(nameof(_gameHwnd));
            }
        }
        #endregion

        private enum StartupOption
        {
            OfficialLauncher,
            MumuSimulator,
            None
        }
        private StartupOption _startupOption = StartupOption.None;

        public static event Action<Bitmap> ScreenshotUpdated;

        private static void OnScreenshotUpdated(ref Bitmap bmp)
        {
            ScreenshotUpdated?.Invoke(bmp);
        }

        public DashboardViewModel(AppSettings settings, ILogger<DashboardViewModel> logger)
        {
            _settings = settings;
            _logger = logger;

            // 初始化时从设置中加载数据
            CaptureInterval = _settings.CaptureInterval;
            RealTimeScreenshotEnabled = _settings.RealTimeScreenshotEnabled;
            LogWindowEnabled = _settings.LogWindowEnabled;
            LogWindowMarqueeEnabled = _settings.LogWindowMarqueeEnabled;
            DebugLogEnabled = _settings.DebugLogEnabled;
            MaskWindowEnabled = _settings.MaskWindowEnabled;

        }

        private void InitializeCaptureTimer()
        {
            _captureTimer = new DispatcherTimer();
            _captureTimer.Interval = TimeSpan.FromMilliseconds(_captureInterval);
        }
        public void InitializeSyncWindowTimer()
        {
            _syncWindowTimer = new DispatcherTimer();
            _syncWindowTimer.Interval = TimeSpan.FromMilliseconds(50);
        }
        private void CaptureTimer_Tick(object? sender, EventArgs e)
        {
            if (_gameHwnd != IntPtr.Zero && _realTimeScreenshotEnabled)
            {
                bmp?.Dispose();
                bmp = null;

                using (var frame = _capture?.Capture())
                {
                    if (frame != null)
                    {
                        bmp = frame.ToBitmap();
                        OnScreenshotUpdated(ref bmp);
                    }
                }
            }
        }
        private void SyncWindowTimer_Tick(object? sender, EventArgs e)
        {
            if (_gameHwnd != IntPtr.Zero)
            {
                if (NativeMethodsService.IsIconic(_gameHwnd)) // 最小化
                {
                    _logWindow?.Hide();
                    _maskWindow?.Hide();
                }
                else if (NativeMethodsService.GetForegroundWindow()!= _displayHwnd) // 由于Mumu模拟器有两个句柄，真正的游戏窗口句柄是子句柄，而且不在顶层，所以需要父句柄
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
        }

        private bool CanStartTrigger() => StartButtonEnabled;

        [RelayCommand(CanExecute = nameof(CanStartTrigger))]
        private void OnStartTrigger()
        {
            GetGameHwnd();

            if (_gameHwnd == IntPtr.Zero)
            {
                var uiMessageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "⚠️ 错误",
                    Content = "未找到游戏窗口。请先启动游戏！",
                    
                };
                _ = uiMessageBox.ShowDialogAsync();
                return;
            } 
            else
            {
                var snackbarInfo = new SnackbarInfo
                {
                    Title = "启动成功",
                    Message = "截图器已启动，可启动其他任务。",
                    Appearance = ControlAppearance.Success,
                    Icon = new SymbolIcon(SymbolRegular.CheckmarkCircle24, 36),
                    Duration = TimeSpan.FromSeconds(3),
                };
                WeakReferenceMessenger.Default.Send(new ShowSnackbarMessage(snackbarInfo));
            }

            StartButtonVisibility = Visibility.Collapsed;
            StopButtonVisibility = Visibility.Visible;

            // 当官方启动器时，将游戏窗口置于前端
            if(_startupOption == StartupOption.OfficialLauncher)
            {
                NativeMethodsService.ShowWindow(_gameHwnd, NativeMethodsService.SW_SHOW);
                NativeMethodsService.SetForegroundWindow(_gameHwnd);
            }

            // 隐藏WPF应用的主窗口。
            Application.Current.Dispatcher.Invoke(() =>
            {
                Application.Current.MainWindow.Hide();
            });

            InitializeCaptureTimer();
            InitializeSyncWindowTimer();

            _captureTimer.Tick += CaptureTimer_Tick;
            _captureTimer.Start();

            // 启动日志窗口
            if (_logWindowEnabled)
            {
                _syncWindowTimer.Tick += SyncWindowTimer_Tick;
                _syncWindowTimer.Start();

                _logWindow = new LogWindow();
                _logWindow.Show();
                _logWindow.ShowInTaskbar = false;   //在ALT+TAB中不显示
                //_logWindow.Owner = GetWindow(_gameHwnd); // 将游戏窗口设置为LogWindow的Owner
                _logWindow.RefreshPosition(_gameHwnd);
                _logWindow.ShowDebugLogs = DebugLogEnabled;
                _logWindow.ShowMarquee = LogWindowMarqueeEnabled;
                _logger.LogInformation("检测到[Yellow]{_startupOption}[/Yellow]已启动", _startupOption);
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

            _logWindow?.Close();
            _logWindow = null;

            _maskWindow?.Close();
            _maskWindow = null;

            _captureTimer.Tick -= CaptureTimer_Tick;
            _captureTimer.Stop();

            _syncWindowTimer.Tick -= SyncWindowTimer_Tick;
            _syncWindowTimer.Stop();

            _capture.Stop();
            _capture = null;
            GC.Collect();

        }


        [RelayCommand]
        public void OnGoToWikiUrl()
        {
            Process.Start(new ProcessStartInfo("https://autohpma-web.vercel.app/") { UseShellExecute = true });
        }

        public void OnNavigatedFrom()
        {
            //throw new NotImplementedException();
        }

        public void OnNavigatedTo()
        {
            //throw new NotImplementedException();
        }

        /**
         * @brief 获取游戏窗口
         * @param hWnd 窗口句柄
         * @return System.Windows.Window 窗口对象
         */
        private System.Windows.Window GetWindow(IntPtr hWnd)
        {
            if (hWnd != IntPtr.Zero)
            {
                var hwndSource = HwndSource.FromHwnd(hWnd);
                if (hwndSource != null)
                {
                    var rootVisual = hwndSource.RootVisual;
                    if (rootVisual != null)
                    {
                        var parent = VisualTreeHelper.GetParent(rootVisual);
                        if (parent is System.Windows.Window window)
                        {
                            return window;
                        }
                    }
                }
            }
            throw new InvalidOperationException("无法获取窗口对象");
        }

        /**
         * @brief 获取游戏窗口句柄（Mumu模拟器或官方启动器）
         * @param void
         * @return void
         * @note 直接对成员变量赋值
         * _displayHwnd：模拟器窗口句柄
         * _gameHwnd：游戏窗口句柄（若为Mumu模拟器，则为子句柄）
         * _startupOption：启动选项（Mumu模拟器或官方启动器）
         */
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

        /**
         * @brief 获取游戏窗口信息
         * @param void
         * @return void
         * @note 输出方式为日志窗口
         * 输出信息包括：
         * 游戏窗口分辨率和坐标
         * Mumu模拟器窗口分辨率和坐标
         */
        private void ShowGameWindowInfo()
        {
            try
            {
                int left, top, width, height;
                int leftMumu, topMumu;
                
                _logger.LogDebug("窗口句柄信息 - DisplayHwnd: [Yellow]0x{DisplayHwnd:X}[/Yellow], GameHwnd: [Yellow]0x{GameHwnd:X}[/Yellow]", _displayHwnd.ToInt64(), _gameHwnd.ToInt64());

                WindowInteractionHelper.GetWindowPositionAndSize(_gameHwnd, out left, out top, out width, out height);
                _logger.LogInformation("检测到游戏窗口分辨率 [Yellow]{Width}*{Height}[/Yellow]", width, height);
                _logger.LogDebug("游戏窗口位置：左上角({Left},{Top})", left, top);
                WindowInteractionHelper.GetWindowPositionAndSize(_displayHwnd, out leftMumu, out topMumu, out width, out height);
                _logger.LogDebug("检测到模拟器窗口分辨率{Width}*{Height}", width, height);
                _logger.LogDebug("模拟器窗口位置：左上角({Left},{Top})", leftMumu, topMumu);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取窗口信息失败");
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        partial void OnCaptureIntervalChanged(int value)
        {
            _settings.CaptureInterval = value;
            _settings.Save();
            if (_captureTimer != null)
            {
                _captureTimer.Interval = TimeSpan.FromMilliseconds(value);
            }
        }

        partial void OnRealTimeScreenshotEnabledChanged(bool value)
        {
            _settings.RealTimeScreenshotEnabled = value;
            _settings.Save();
        }

        partial void OnLogWindowEnabledChanged(bool value)
        {
            _settings.LogWindowEnabled = value;
            _settings.Save();
        }

        partial void OnDebugLogEnabledChanged(bool value)
        {
            _settings.DebugLogEnabled = value;
            _settings.Save();
        }

        partial void OnMaskWindowEnabledChanged(bool value)
        {
            _settings.MaskWindowEnabled = value;
            _settings.Save();
        }

        partial void OnLogWindowMarqueeEnabledChanged(bool value)
        {
            _settings.LogWindowMarqueeEnabled = value;
            _settings.Save();
        }

    }
}
